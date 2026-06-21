using AutoStock.Repositories;
using AutoStock.Repositories.Entities;
using AutoStock.Repositories.Enums;
using AutoStock.Services.Dtos.AuditLogs;
using AutoStock.Services.Dtos.Common;
using AutoStock.Services.Dtos.CurrentAccounts;
using AutoStock.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AutoStock.Services.Services
{
    public class CurrentAccountService : ICurrentAccountService
    {
        private const string PaymentCancellationDocumentPrefix = "PAY-CANCEL-";

        private readonly AppDbContext _context;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IAuditLogService _auditLogService;

        public CurrentAccountService(
            AppDbContext context,
            IDateTimeProvider dateTimeProvider,
            IAuditLogService auditLogService)
        {
            _context = context;
            _dateTimeProvider = dateTimeProvider;
            _auditLogService = auditLogService;
        }

        public async Task<ServiceResult<bool>> CreatePaymentAsync(CreatePaymentRequestDto request, int workshopId)
        {
            if (request.Amount <= 0)
                return ServiceResult<bool>.Fail("Tahsilat tutarı 0'dan büyük olmalıdır.");

            if (request.PaymentDate.HasValue && request.PaymentDate.Value > _dateTimeProvider.Now.AddDays(1))
                return ServiceResult<bool>.Fail("Tahsilat tarihi ileri bir tarih olamaz.");

            var customer = await _context.Customers
                .AsNoTracking()
                .Where(x =>
                    x.Id == request.CustomerId &&
                    x.WorkshopId == workshopId)
                .Select(x => new
                {
                    x.Id,
                    x.FullName,
                    x.CompanyName,
                    x.PhoneNumber
                })
                .FirstOrDefaultAsync();

            if (customer is null)
                return ServiceResult<bool>.Fail("Müşteri bulunamadı.");

            var customerName = ResolveCustomerName(customer.FullName, customer.CompanyName);

            Invoice? invoice = null;
            decimal invoiceRemainingAmount = 0;

            if (request.InvoiceId.HasValue)
            {
                invoice = await _context.Invoices
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.Id == request.InvoiceId.Value &&
                        x.WorkshopId == workshopId &&
                        x.CustomerId == request.CustomerId);

                if (invoice is null)
                    return ServiceResult<bool>.Fail("Tahsilat bağlanacak fatura bulunamadı.");

                if (invoice.Status == InvoiceStatus.Draft)
                    return ServiceResult<bool>.Fail("Taslak faturaya tahsilat girilemez. Önce faturayı kesin.");

                if (invoice.Status == InvoiceStatus.Cancelled)
                    return ServiceResult<bool>.Fail("İptal edilmiş faturaya tahsilat girilemez.");

                var invoicePaidTotal = await GetInvoicePaidTotalAsync(invoice.Id, request.CustomerId, workshopId);
                invoiceRemainingAmount = invoice.GrandTotal - invoicePaidTotal;

                if (invoiceRemainingAmount <= 0)
                    return ServiceResult<bool>.Fail("Bu faturanın kalan tahsilat tutarı bulunmuyor.");

                if (request.Amount > invoiceRemainingAmount)
                {
                    return ServiceResult<bool>.Fail(
                        $"Tahsilat tutarı kalan fatura tutarını aşamaz. Kalan: {invoiceRemainingAmount:N2} TL");
                }
            }

            await using var dbTransaction = await _context.Database.BeginTransactionAsync();

            var transaction = new CurrentAccountTransaction
            {
                WorkshopId = workshopId,
                CustomerId = request.CustomerId,
                InvoiceId = invoice?.Id,

                Type = CurrentAccountTransactionType.Payment,

                Debit = 0,
                Credit = request.Amount,

                TransactionDate = request.PaymentDate ?? _dateTimeProvider.Now,

                Description = BuildPaymentDescription(request, invoice),
                DocumentNumber = invoice?.InvoiceNumber,

                IsSystemGenerated = false,
                CreatedAt = _dateTimeProvider.Now
            };

            _context.CurrentAccountTransactions.Add(transaction);

            await _context.SaveChangesAsync();

            await _auditLogService.AddAsync(new AuditLogCreateDto
            {
                WorkshopId = workshopId,
                ActionType = AuditActionType.ReceivePayment,
                EntityType = AuditEntityType.CurrentAccountTransaction,
                EntityId = transaction.Id,
                Description = invoice is null
                    ? $"Tahsilat girdi: {customerName} / {transaction.Credit:N2} TL"
                    : $"Faturaya tahsilat girdi: {invoice.InvoiceNumber} / {customerName} / {transaction.Credit:N2} TL",
                NewValues = new
                {
                    transaction.Id,
                    transaction.CustomerId,
                    CustomerName = customerName,
                    transaction.InvoiceId,
                    InvoiceNumber = invoice?.InvoiceNumber,
                    Type = transaction.Type.ToString(),
                    Amount = transaction.Credit,
                    transaction.TransactionDate,
                    transaction.Description,
                    transaction.DocumentNumber,
                    transaction.IsSystemGenerated,
                    InvoiceRemainingBeforePayment = invoice is null ? (decimal?)null : invoiceRemainingAmount
                }
            });

            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            return ServiceResult<bool>.Success(true);
        }

        public async Task<ServiceResult<bool>> CancelPaymentAsync(int transactionId, CancelPaymentRequestDto request, int workshopId)
        {
            if (transactionId <= 0)
                return ServiceResult<bool>.Fail("Geçerli bir tahsilat hareketi seçiniz.");

            var reason = NormalizeNullable(request?.Reason);

            if (reason is { Length: > 500 })
                return ServiceResult<bool>.Fail("İptal açıklaması en fazla 500 karakter olabilir.");

            var payment = await _context.CurrentAccountTransactions
                .Include(x => x.Customer)
                .Include(x => x.Invoice)
                .FirstOrDefaultAsync(x =>
                    x.Id == transactionId &&
                    x.WorkshopId == workshopId);

            if (payment is null)
                return ServiceResult<bool>.Fail("Tahsilat hareketi bulunamadı.");

            if (payment.Type != CurrentAccountTransactionType.Payment || payment.Credit <= 0)
                return ServiceResult<bool>.Fail("Sadece tahsilat hareketleri iptal edilebilir.");

            if (payment.IsSystemGenerated)
                return ServiceResult<bool>.Fail("Sistem tarafından oluşturulan hesap hareketleri bu ekrandan iptal edilemez.");

            var cancellationDocumentNumber = BuildPaymentCancellationDocumentNumber(payment.Id);

            var alreadyCancelled = await _context.CurrentAccountTransactions
                .AsNoTracking()
                .AnyAsync(x =>
                    x.WorkshopId == workshopId &&
                    x.Type == CurrentAccountTransactionType.Cancel &&
                    x.DocumentNumber == cancellationDocumentNumber);

            if (alreadyCancelled)
                return ServiceResult<bool>.Fail("Bu tahsilat daha önce iptal edilmiş.");

            await using var dbTransaction = await _context.Database.BeginTransactionAsync();

            var cancellationTransaction = new CurrentAccountTransaction
            {
                WorkshopId = payment.WorkshopId,
                CustomerId = payment.CustomerId,
                InvoiceId = payment.InvoiceId,

                Type = CurrentAccountTransactionType.Cancel,

                Debit = payment.Credit,
                Credit = 0,

                TransactionDate = _dateTimeProvider.Now,

                Description = BuildPaymentCancellationDescription(payment, reason),
                DocumentNumber = cancellationDocumentNumber,

                IsSystemGenerated = false,
                CreatedAt = _dateTimeProvider.Now
            };

            _context.CurrentAccountTransactions.Add(cancellationTransaction);

            await _context.SaveChangesAsync();

            var customerName = ResolveCustomerName(payment.Customer?.FullName, payment.Customer?.CompanyName);
            var invoiceNumber = payment.Invoice?.InvoiceNumber ?? payment.DocumentNumber;

            await _auditLogService.AddAsync(new AuditLogCreateDto
            {
                WorkshopId = workshopId,
                ActionType = AuditActionType.Cancel,
                EntityType = AuditEntityType.CurrentAccountTransaction,
                EntityId = cancellationTransaction.Id,
                Description = invoiceNumber is null
                    ? $"Tahsilat iptal edildi: {customerName} / {payment.Credit:N2} TL"
                    : $"Tahsilat iptal edildi: {invoiceNumber} / {customerName} / {payment.Credit:N2} TL",
                OldValues = new
                {
                    PaymentTransactionId = payment.Id,
                    payment.CustomerId,
                    CustomerName = customerName,
                    payment.InvoiceId,
                    InvoiceNumber = invoiceNumber,
                    Amount = payment.Credit,
                    payment.TransactionDate,
                    payment.Description,
                    payment.DocumentNumber
                },
                NewValues = new
                {
                    CancellationTransactionId = cancellationTransaction.Id,
                    cancellationTransaction.Type,
                    ReversalDebit = cancellationTransaction.Debit,
                    cancellationTransaction.TransactionDate,
                    cancellationTransaction.Description,
                    cancellationTransaction.DocumentNumber,
                    Reason = reason
                }
            });

            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            return ServiceResult<bool>.Success(true);
        }

        public async Task<ServiceResult<GetCustomerCurrentAccountResponseDto>> GetCustomerAccountAsync(int customerId, int workshopId)
        {
            var customer = await _context.Customers
                .AsNoTracking()
                .Where(x => x.Id == customerId && x.WorkshopId == workshopId)
                .Select(x => new
                {
                    x.Id,
                    x.FullName,
                    x.CompanyName,
                    x.PhoneNumber
                })
                .FirstOrDefaultAsync();

            if (customer is null)
                return ServiceResult<GetCustomerCurrentAccountResponseDto>.Fail("Müşteri bulunamadı.");

            var transactions = await _context.CurrentAccountTransactions
                .AsNoTracking()
                .Include(x => x.Invoice)
                .Where(x => x.CustomerId == customerId && x.WorkshopId == workshopId)
                .OrderBy(x => x.TransactionDate)
                .ThenBy(x => x.Id)
                .ToListAsync();

            var cancelledPaymentIds = ResolveCancelledPaymentIds(transactions);

            decimal runningBalance = 0;

            var transactionDtos = transactions
                .Select(x =>
                {
                    runningBalance += x.Debit - x.Credit;

                    var isPaymentCancellation = IsPaymentCancellationTransaction(x);
                    var isPaymentCancelled = x.Type == CurrentAccountTransactionType.Payment && cancelledPaymentIds.Contains(x.Id);

                    return new CurrentAccountTransactionDto
                    {
                        Id = x.Id,
                        TransactionDate = x.TransactionDate,
                        Type = (int)x.Type,
                        TypeText = GetTransactionTypeText(x),
                        Description = x.Description,
                        DocumentNumber = x.DocumentNumber,
                        InvoiceId = x.InvoiceId,
                        InvoiceNumber = x.Invoice?.InvoiceNumber ?? x.DocumentNumber,
                        Debit = x.Debit,
                        Credit = x.Credit,
                        Balance = runningBalance,
                        IsSystemGenerated = x.IsSystemGenerated,
                        IsPaymentCancellation = isPaymentCancellation,
                        IsPaymentCancelled = isPaymentCancelled,
                        CanCancelPayment =
                            x.Type == CurrentAccountTransactionType.Payment &&
                            x.Credit > 0 &&
                            !x.IsSystemGenerated &&
                            !isPaymentCancelled
                    };
                })
                .ToList();

            var issuedInvoices = await _context.Invoices
                .AsNoTracking()
                .Where(x =>
                    x.WorkshopId == workshopId &&
                    x.CustomerId == customerId &&
                    x.Status == InvoiceStatus.Issued)
                .OrderByDescending(x => x.InvoiceDate)
                .ThenByDescending(x => x.Id)
                .Select(x => new
                {
                    InvoiceId = x.Id,
                    x.ServiceRecordId,
                    x.InvoiceNumber,
                    x.InvoiceDate,
                    x.Plate,
                    x.GrandTotal
                })
                .ToListAsync();

            var issuedInvoiceIds = issuedInvoices
                .Select(x => x.InvoiceId)
                .ToList();

            var invoicePayments = issuedInvoiceIds.Any()
                ? await GetInvoicePaidTotalsAsync(issuedInvoiceIds, customerId, workshopId)
                : new Dictionary<int, decimal>();

            var openInvoices = issuedInvoices
                .Select(x =>
                {
                    invoicePayments.TryGetValue(x.InvoiceId, out var paidTotal);
                    paidTotal = Math.Max(0m, paidTotal);

                    var remainingAmount = x.GrandTotal - paidTotal;

                    return new CurrentAccountOpenInvoiceDto
                    {
                        InvoiceId = x.InvoiceId,
                        ServiceRecordId = x.ServiceRecordId,
                        InvoiceNumber = x.InvoiceNumber,
                        InvoiceDate = x.InvoiceDate,
                        Plate = x.Plate,
                        GrandTotal = x.GrandTotal,
                        PaidTotal = paidTotal,
                        RemainingAmount = remainingAmount
                    };
                })
                .Where(x => x.RemainingAmount > 0)
                .OrderByDescending(x => x.InvoiceDate)
                .ThenByDescending(x => x.InvoiceId)
                .ToList();

            var invoiceTotal = transactions
                .Where(x => x.Type == CurrentAccountTransactionType.InvoiceDebit)
                .Sum(x => x.Debit);

            var paymentTotal = transactions
                .Where(x => x.Type == CurrentAccountTransactionType.Payment)
                .Sum(x => x.Credit)
                - transactions
                    .Where(IsPaymentCancellationTransaction)
                    .Sum(x => x.Debit);

            var activePaymentTransactionIds = transactions
                .Where(x => x.Type == CurrentAccountTransactionType.Payment && !cancelledPaymentIds.Contains(x.Id))
                .Select(x => x.Id)
                .ToHashSet();

            var response = new GetCustomerCurrentAccountResponseDto
            {
                CustomerId = customer.Id,
                CustomerName = ResolveCustomerName(customer.FullName, customer.CompanyName),
                CustomerPhone = customer.PhoneNumber,
                Balance = runningBalance,
                InvoiceTotal = invoiceTotal,
                PaymentTotal = Math.Max(0m, paymentTotal),
                OpenInvoiceTotal = openInvoices.Sum(x => x.RemainingAmount),
                OpenInvoiceCount = openInvoices.Count,
                LastPaymentDate = transactions
                    .Where(x => activePaymentTransactionIds.Contains(x.Id))
                    .OrderByDescending(x => x.TransactionDate)
                    .Select(x => (DateTime?)x.TransactionDate)
                    .FirstOrDefault(),
                OpenInvoices = openInvoices,
                Transactions = transactionDtos
            };

            return ServiceResult<GetCustomerCurrentAccountResponseDto>.Success(response);
        }

        public async Task<ServiceResult<CurrentAccountSummaryDto>> GetSummaryAsync(int workshopId)
        {
            var now = _dateTimeProvider.Now;

            var monthStart = new DateTime(now.Year, now.Month, 1);
            var nextMonthStart = monthStart.AddMonths(1);

            var thisMonthInvoiceTotal = await _context.CurrentAccountTransactions
                .AsNoTracking()
                .Where(x =>
                    x.WorkshopId == workshopId &&
                    x.TransactionDate >= monthStart &&
                    x.TransactionDate < nextMonthStart &&
                    (
                        x.Type == CurrentAccountTransactionType.InvoiceDebit ||
                        (x.Type == CurrentAccountTransactionType.Cancel && x.Credit > 0)
                    ))
                .SumAsync(x => x.Debit - x.Credit);

            var thisMonthPaymentCreditTotal = await _context.CurrentAccountTransactions
                .AsNoTracking()
                .Where(x =>
                    x.WorkshopId == workshopId &&
                    x.Type == CurrentAccountTransactionType.Payment &&
                    x.TransactionDate >= monthStart &&
                    x.TransactionDate < nextMonthStart)
                .SumAsync(x => x.Credit);

            var thisMonthPaymentCancelTotal = await _context.CurrentAccountTransactions
                .AsNoTracking()
                .Where(x =>
                    x.WorkshopId == workshopId &&
                    x.Type == CurrentAccountTransactionType.Cancel &&
                    x.Debit > 0 &&
                    x.DocumentNumber != null &&
                    x.DocumentNumber.StartsWith(PaymentCancellationDocumentPrefix) &&
                    x.TransactionDate >= monthStart &&
                    x.TransactionDate < nextMonthStart)
                .SumAsync(x => x.Debit);

            var thisMonthPaymentTotal = thisMonthPaymentCreditTotal - thisMonthPaymentCancelTotal;

            var customerBalances = await _context.CurrentAccountTransactions
                .AsNoTracking()
                .Where(x => x.WorkshopId == workshopId)
                .GroupBy(x => new
                {
                    x.CustomerId,
                    x.Customer.FullName,
                    x.Customer.CompanyName
                })
                .Select(g => new CustomerBalanceSummaryDto
                {
                    CustomerId = g.Key.CustomerId,
                    CustomerName =
                        !string.IsNullOrWhiteSpace(g.Key.FullName)
                            ? g.Key.FullName
                            : g.Key.CompanyName ?? "İsimsiz Müşteri",
                    Balance = g.Sum(x => x.Debit - x.Credit)
                })
                .Where(x => x.Balance != 0)
                .OrderByDescending(x => x.Balance)
                .ToListAsync();

            var totalReceivableBalance = customerBalances
                .Where(x => x.Balance > 0)
                .Sum(x => x.Balance);

            var response = new CurrentAccountSummaryDto
            {
                ThisMonthInvoiceTotal = thisMonthInvoiceTotal,
                ThisMonthPaymentTotal = Math.Max(0m, thisMonthPaymentTotal),
                TotalReceivableBalance = totalReceivableBalance,
                CustomerBalances = customerBalances
            };

            return ServiceResult<CurrentAccountSummaryDto>.Success(response);
        }

        public async Task<ServiceResult<CurrentAccountPagedSummaryDto>> GetPagedSummaryAsync(CurrentAccountListQueryDto query, int workshopId)
        {
            query ??= new CurrentAccountListQueryDto();
            query.Normalize();

            var now = _dateTimeProvider.Now;

            var monthStart = new DateTime(now.Year, now.Month, 1);
            var nextMonthStart = monthStart.AddMonths(1);

            var thisMonthInvoiceTotal = await _context.CurrentAccountTransactions
                .AsNoTracking()
                .Where(x =>
                    x.WorkshopId == workshopId &&
                    x.TransactionDate >= monthStart &&
                    x.TransactionDate < nextMonthStart &&
                    (
                        x.Type == CurrentAccountTransactionType.InvoiceDebit ||
                        (x.Type == CurrentAccountTransactionType.Cancel && x.Credit > 0)
                    ))
                .SumAsync(x => x.Debit - x.Credit);

            var thisMonthPaymentCreditTotal = await _context.CurrentAccountTransactions
                .AsNoTracking()
                .Where(x =>
                    x.WorkshopId == workshopId &&
                    x.Type == CurrentAccountTransactionType.Payment &&
                    x.TransactionDate >= monthStart &&
                    x.TransactionDate < nextMonthStart)
                .SumAsync(x => x.Credit);

            var thisMonthPaymentCancelTotal = await _context.CurrentAccountTransactions
                .AsNoTracking()
                .Where(x =>
                    x.WorkshopId == workshopId &&
                    x.Type == CurrentAccountTransactionType.Cancel &&
                    x.Debit > 0 &&
                    x.DocumentNumber != null &&
                    x.DocumentNumber.StartsWith(PaymentCancellationDocumentPrefix) &&
                    x.TransactionDate >= monthStart &&
                    x.TransactionDate < nextMonthStart)
                .SumAsync(x => x.Debit);

            var thisMonthPaymentTotal = thisMonthPaymentCreditTotal - thisMonthPaymentCancelTotal;

            var totalReceivableBalance = await _context.CurrentAccountTransactions
                .AsNoTracking()
                .Where(x => x.WorkshopId == workshopId)
                .GroupBy(x => x.CustomerId)
                .Select(g => g.Sum(x => x.Debit - x.Credit))
                .Where(balance => balance > 0)
                .SumAsync();

            var transactionsQuery = _context.CurrentAccountTransactions
                .AsNoTracking()
                .Where(x => x.WorkshopId == workshopId);

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = $"%{query.Search}%";

                transactionsQuery = transactionsQuery.Where(x =>
                    EF.Functions.Like(x.Customer.FullName ?? string.Empty, search) ||
                    EF.Functions.Like(x.Customer.CompanyName ?? string.Empty, search) ||
                    EF.Functions.Like(x.Customer.PhoneNumber ?? string.Empty, search));
            }

            var customerBalancesQuery = transactionsQuery
                .GroupBy(x => new
                {
                    x.CustomerId,
                    x.Customer.FullName,
                    x.Customer.CompanyName
                })
                .Select(g => new CustomerBalanceSummaryDto
                {
                    CustomerId = g.Key.CustomerId,
                    CustomerName =
                        !string.IsNullOrWhiteSpace(g.Key.FullName)
                            ? g.Key.FullName
                            : g.Key.CompanyName ?? "İsimsiz Müşteri",

                    Balance = g.Sum(x => x.Debit - x.Credit)
                })
                .Where(x => x.Balance != 0);

            var totalCount = await customerBalancesQuery.CountAsync();

            var customerBalances = await customerBalancesQuery
                .OrderByDescending(x => x.Balance)
                .ThenBy(x => x.CustomerName)
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            var response = new CurrentAccountPagedSummaryDto
            {
                ThisMonthInvoiceTotal = thisMonthInvoiceTotal,
                ThisMonthPaymentTotal = Math.Max(0m, thisMonthPaymentTotal),
                TotalReceivableBalance = totalReceivableBalance,
                CustomerBalances = new PagedResult<CustomerBalanceSummaryDto>
                {
                    Items = customerBalances,
                    PageNumber = query.PageNumber,
                    PageSize = query.PageSize,
                    TotalCount = totalCount
                }
            };

            return ServiceResult<CurrentAccountPagedSummaryDto>.Success(response);
        }

        private async Task<decimal> GetInvoicePaidTotalAsync(int invoiceId, int customerId, int workshopId)
        {
            var paymentCreditTotal = await _context.CurrentAccountTransactions
                .AsNoTracking()
                .Where(x =>
                    x.WorkshopId == workshopId &&
                    x.CustomerId == customerId &&
                    x.InvoiceId == invoiceId &&
                    x.Type == CurrentAccountTransactionType.Payment)
                .SumAsync(x => x.Credit);

            var paymentCancelDebitTotal = await _context.CurrentAccountTransactions
                .AsNoTracking()
                .Where(x =>
                    x.WorkshopId == workshopId &&
                    x.CustomerId == customerId &&
                    x.InvoiceId == invoiceId &&
                    x.Type == CurrentAccountTransactionType.Cancel &&
                    x.Debit > 0 &&
                    x.DocumentNumber != null &&
                    x.DocumentNumber.StartsWith(PaymentCancellationDocumentPrefix))
                .SumAsync(x => x.Debit);

            return Math.Max(0m, paymentCreditTotal - paymentCancelDebitTotal);
        }

        private async Task<Dictionary<int, decimal>> GetInvoicePaidTotalsAsync(List<int> invoiceIds, int customerId, int workshopId)
        {
            var movements = await _context.CurrentAccountTransactions
                .AsNoTracking()
                .Where(x =>
                    x.WorkshopId == workshopId &&
                    x.CustomerId == customerId &&
                    x.InvoiceId.HasValue &&
                    invoiceIds.Contains(x.InvoiceId.Value) &&
                    (
                        x.Type == CurrentAccountTransactionType.Payment ||
                        (x.Type == CurrentAccountTransactionType.Cancel && x.Debit > 0 && x.DocumentNumber != null && x.DocumentNumber.StartsWith(PaymentCancellationDocumentPrefix))
                    ))
                .Select(x => new
                {
                    InvoiceId = x.InvoiceId!.Value,
                    x.Type,
                    x.Debit,
                    x.Credit
                })
                .ToListAsync();

            return movements
                .GroupBy(x => x.InvoiceId)
                .ToDictionary(
                    g => g.Key,
                    g => Math.Max(0m,
                        g.Where(x => x.Type == CurrentAccountTransactionType.Payment).Sum(x => x.Credit) -
                        g.Where(x => x.Type == CurrentAccountTransactionType.Cancel).Sum(x => x.Debit)));
        }

        private static HashSet<int> ResolveCancelledPaymentIds(IEnumerable<CurrentAccountTransaction> transactions)
        {
            var ids = new HashSet<int>();

            foreach (var transaction in transactions)
            {
                if (!IsPaymentCancellationTransaction(transaction))
                    continue;

                if (TryGetCancelledPaymentTransactionId(transaction.DocumentNumber, out var paymentTransactionId))
                {
                    ids.Add(paymentTransactionId);
                }
            }

            return ids;
        }

        private static bool IsPaymentCancellationTransaction(CurrentAccountTransaction transaction)
        {
            return transaction.Type == CurrentAccountTransactionType.Cancel &&
                   transaction.Debit > 0 &&
                   !string.IsNullOrWhiteSpace(transaction.DocumentNumber) &&
                   transaction.DocumentNumber.StartsWith(PaymentCancellationDocumentPrefix, StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryGetCancelledPaymentTransactionId(string? documentNumber, out int transactionId)
        {
            transactionId = 0;

            if (string.IsNullOrWhiteSpace(documentNumber) ||
                !documentNumber.StartsWith(PaymentCancellationDocumentPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var rawId = documentNumber[PaymentCancellationDocumentPrefix.Length..];

            return int.TryParse(rawId, out transactionId) && transactionId > 0;
        }

        private static string BuildPaymentCancellationDocumentNumber(int paymentTransactionId)
        {
            return $"{PaymentCancellationDocumentPrefix}{paymentTransactionId}";
        }

        private static string ResolveCustomerName(string? fullName, string? companyName)
        {
            return !string.IsNullOrWhiteSpace(fullName)
                ? fullName
                : companyName ?? "İsimsiz Müşteri";
        }

        private static string GetTransactionTypeText(CurrentAccountTransaction transaction)
        {
            if (transaction.Type == CurrentAccountTransactionType.Cancel && transaction.Debit > 0)
                return "Tahsilat İptali";

            if (transaction.Type == CurrentAccountTransactionType.Cancel && transaction.Credit > 0)
                return "Fatura İptali";

            return transaction.Type switch
            {
                CurrentAccountTransactionType.InvoiceDebit => "Fatura",
                CurrentAccountTransactionType.Payment => "Tahsilat",
                CurrentAccountTransactionType.Refund => "İade",
                CurrentAccountTransactionType.Adjustment => "Düzeltme",
                CurrentAccountTransactionType.Cancel => "İptal",
                _ => transaction.Type.ToString()
            };
        }

        private static string BuildPaymentDescription(CreatePaymentRequestDto request, Invoice? invoice)
        {
            var paymentMethod = NormalizeNullable(request.PaymentMethod);
            var userDescription = NormalizeNullable(request.Description);

            if (!string.IsNullOrWhiteSpace(userDescription))
            {
                return string.IsNullOrWhiteSpace(paymentMethod)
                    ? userDescription
                    : $"{userDescription} ({paymentMethod})";
            }

            if (invoice is not null)
            {
                return string.IsNullOrWhiteSpace(paymentMethod)
                    ? $"{invoice.InvoiceNumber} numaralı fatura tahsilatı"
                    : $"{invoice.InvoiceNumber} numaralı fatura tahsilatı ({paymentMethod})";
            }

            return string.IsNullOrWhiteSpace(paymentMethod)
                ? "Tahsilat"
                : $"Tahsilat ({paymentMethod})";
        }

        private static string BuildPaymentCancellationDescription(CurrentAccountTransaction payment, string? reason)
        {
            var baseDescription = payment.Invoice is not null
                ? $"{payment.Invoice.InvoiceNumber} numaralı fatura tahsilatı iptali"
                : $"#{payment.Id} numaralı tahsilat iptali";

            return string.IsNullOrWhiteSpace(reason)
                ? baseDescription
                : $"{baseDescription} - {reason.Trim()}";
        }

        private static string? NormalizeNullable(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }
    }
}
