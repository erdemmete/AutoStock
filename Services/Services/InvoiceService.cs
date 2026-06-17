using AutoStock.Repositories;
using AutoStock.Repositories.Entities;
using AutoStock.Repositories.Enums;
using AutoStock.Services.Dtos.AuditLogs;
using AutoStock.Services.Dtos.Common;
using AutoStock.Services.Dtos.Invoices;
using AutoStock.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Services.Interfaces.StockItems;

namespace AutoStock.Services.Services
{
    public class InvoiceService : IInvoiceService
    {
        private readonly AppDbContext _context;
        private readonly IStockItemService _stockItemService;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IAuditLogService _auditLogService;

        public InvoiceService(
            AppDbContext context,
            IStockItemService stockItemService,
            IDateTimeProvider dateTimeProvider,
            IAuditLogService auditLogService)
        {
            _context = context;
            _stockItemService = stockItemService;
            _dateTimeProvider = dateTimeProvider;
            _auditLogService = auditLogService;
        }

        public async Task<ServiceResult<CreateInvoiceDraftDto>> GetCreateDraftAsync(int serviceRecordId, int workshopId)
        {
            var serviceRecord = await _context.ServiceRecords
                .Include(x => x.Customer)
                .Include(x => x.Vehicle)
                .Include(x => x.Operations)
                .FirstOrDefaultAsync(x =>
                    x.Id == serviceRecordId &&
                    x.WorkshopId == workshopId);

            if (serviceRecord is null)
                return ServiceResult<CreateInvoiceDraftDto>.Fail("Servis kaydı bulunamadı.");

            var customer = serviceRecord.Customer;

            var customerTitle =
                !string.IsNullOrWhiteSpace(customer.FullName)
                    ? customer.FullName
                    : customer.CompanyName ?? serviceRecord.CustomerNameSnapshot;

            var addressParts = new[]
            {
                customer.Address,
                customer.AddressDistrict,
                customer.AddressCity
            }
            .Where(x => !string.IsNullOrWhiteSpace(x));

            var dto = new CreateInvoiceDraftDto
            {
                ServiceRecordId = serviceRecord.Id,
                CustomerId = customer.Id,

                CustomerTitle = customerTitle,
                CustomerTaxOffice = customer.TaxOffice,
                CustomerTaxNumber = customer.TaxNumber,
                CustomerTckn = customer.NationalIdentityNumber,
                CustomerAddress = string.Join(" / ", addressParts),

                Plate = serviceRecord.VehiclePlateSnapshot,
                ChassisNumber = serviceRecord.Vehicle?.ChassisNumber,
                VehicleBrandName = serviceRecord.VehicleBrandNameSnapshot,
                VehicleModelName = serviceRecord.VehicleModelNameSnapshot,
                VehicleModelYear = serviceRecord.Vehicle?.ModelYear,
                Mileage = serviceRecord.MileageSnapshot
            };

            foreach (var operation in serviceRecord.Operations.Where(x => !x.IsDeleted))
            {
                dto.Items.Add(new CreateInvoiceDraftItemDto
                {
                    ItemType = operation.Type == OperationType.Part
                        ? (int)InvoiceItemType.Part
                        : (int)InvoiceItemType.Labor,

                    Description = operation.Description,
                    Quantity = operation.Quantity,
                    StockItemId = operation.StockItemId,
                    Unit = "Adet",
                    UnitPrice = operation.UnitPrice,
                    DiscountRate = 0,
                    VatRate = 20
                });
            }

            return ServiceResult<CreateInvoiceDraftDto>.Success(dto);
        }

        public async Task<ServiceResult<CreateInvoiceResponseDto>> CreateAsync(CreateInvoiceDto request, int workshopId)
        {
            if (request.Items is null || !request.Items.Any())
                return ServiceResult<CreateInvoiceResponseDto>.Fail("Fatura kalemi zorunludur.");

            var serviceRecord = await _context.ServiceRecords
                .FirstOrDefaultAsync(x =>
                    x.Id == request.ServiceRecordId &&
                    x.WorkshopId == workshopId);

            if (serviceRecord is null)
                return ServiceResult<CreateInvoiceResponseDto>.Fail("Servis kaydı bulunamadı.");

            var validItems = request.Items
                .Where(x =>
                    !string.IsNullOrWhiteSpace(x.Description) &&
                    x.Quantity > 0)
                .ToList();

            var stockItemIds = validItems
                .Where(x => x.StockItemId.HasValue)
                .Select(x => x.StockItemId!.Value)
                .Distinct()
                .ToList();

            if (stockItemIds.Any())
            {
                var validStockItemCount = await _context.StockItems
                    .CountAsync(x =>
                        stockItemIds.Contains(x.Id) &&
                        x.WorkshopId == workshopId &&
                        x.IsActive);

                if (validStockItemCount != stockItemIds.Count)
                    return ServiceResult<CreateInvoiceResponseDto>.Fail(
                        "Fatura kalemlerinde geçersiz stok kartı seçimi var.");
            }

            if (!validItems.Any())
                return ServiceResult<CreateInvoiceResponseDto>.Fail("Geçerli fatura kalemi bulunamadı.");

            await using var transaction = await _context.Database.BeginTransactionAsync();

            decimal subTotal = 0;
            decimal discountTotal = 0;
            decimal vatTotal = 0;
            decimal grandTotal = 0;

            var invoice = new Invoice
            {
                WorkshopId = workshopId,
                ServiceRecordId = request.ServiceRecordId,
                CustomerId = serviceRecord.CustomerId,

                Type = InvoiceType.Manual,
                Status = InvoiceStatus.Draft,

                InvoiceNumber = $"MAT-{_dateTimeProvider.Now:yyyyMMddHHmmss}",
                InvoiceDate = _dateTimeProvider.Now,

                CustomerTitle = string.IsNullOrWhiteSpace(request.CustomerTitle)
                 ? "Bilinmeyen Müşteri"
                 : request.CustomerTitle.Trim(),

                CustomerTaxOffice = request.CustomerTaxOffice,
                CustomerTaxNumber = request.CustomerTaxNumber,
                CustomerTckn = request.CustomerTckn,
                CustomerAddress = request.CustomerAddress,

                Plate = request.Plate,
                ChassisNumber = request.ChassisNumber,
                VehicleBrandName = request.VehicleBrandName,
                VehicleModelName = request.VehicleModelName,
                VehicleModelYear = request.VehicleModelYear,
                Mileage = request.Mileage,

                CreatedAt = _dateTimeProvider.Now
            };

            foreach (var item in validItems)
            {
                var quantity = item.Quantity;
                var unitPrice = item.UnitPrice;
                var discountRate = item.DiscountRate;
                var vatRate = item.VatRate;

                if (discountRate < 0) discountRate = 0;
                if (vatRate < 0) vatRate = 0;

                var lineSubTotal = quantity * unitPrice;
                var discountAmount = lineSubTotal * discountRate / 100;
                var taxableAmount = lineSubTotal - discountAmount;
                var vatAmount = taxableAmount * vatRate / 100;
                var lineTotal = taxableAmount + vatAmount;

                subTotal += lineSubTotal;
                discountTotal += discountAmount;
                vatTotal += vatAmount;
                grandTotal += lineTotal;

                invoice.Items.Add(new InvoiceItem
                {
                    ItemType = (InvoiceItemType)item.ItemType,

                    Description = item.Description!.Trim(),
                    Quantity = quantity,
                    Unit = string.IsNullOrWhiteSpace(item.Unit) ? "Adet" : item.Unit.Trim(),

                    UnitPrice = unitPrice,
                    DiscountRate = discountRate,
                    DiscountAmount = discountAmount,

                    VatRate = vatRate,
                    VatAmount = vatAmount,

                    LineTotal = lineTotal,
                    StockItemId = item.StockItemId
                });
            }

            invoice.Subtotal = subTotal;
            invoice.DiscountTotal = discountTotal;
            invoice.VatTotal = vatTotal;
            invoice.GrandTotal = grandTotal;

            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();

            await _auditLogService.AddAsync(new AuditLogCreateDto
            {
                WorkshopId = workshopId,
                ActionType = AuditActionType.Create,
                EntityType = AuditEntityType.Invoice,
                EntityId = invoice.Id,
                Description = $"Fatura taslağı oluşturuldu: {GetInvoiceDisplayName(invoice)}",
                NewValues = GetInvoiceAuditValues(invoice)
            });

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return ServiceResult<CreateInvoiceResponseDto>.Success(new CreateInvoiceResponseDto
            {
                InvoiceId = invoice.Id,
                ServiceRecordId = invoice.ServiceRecordId ?? 0,
                InvoiceNumber = invoice.InvoiceNumber,
                GrandTotal = invoice.GrandTotal
            });
        }

        public async Task<ServiceResult<InvoiceDetailDto>> GetDetailAsync(int invoiceId, int workshopId)
        {
            var invoice = await _context.Invoices
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x =>
                    x.Id == invoiceId &&
                    x.WorkshopId == workshopId);

            if (invoice is null)
                return ServiceResult<InvoiceDetailDto>.Fail("Fatura bulunamadı.");

            var workshopProfile = await _context.Set<WorkshopProfile>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.WorkshopId == workshopId);

            var bankAccounts = await _context.Set<WorkshopBankAccount>()
                .AsNoTracking()
                .Where(x =>
                    x.WorkshopId == workshopId &&
                    x.IsActive &&
                    x.ShowOnInvoices)
                .OrderByDescending(x => x.IsDefault)
                .ThenBy(x => x.SortOrder)
                .ThenBy(x => x.BankName)
                .Select(x => new InvoiceBankAccountDto
                {
                    Id = x.Id,
                    BankName = x.BankName,
                    AccountHolder = x.AccountHolder,
                    Iban = x.Iban,
                    CurrencyCode = x.CurrencyCode,
                    BranchName = x.BranchName,
                    AccountNumber = x.AccountNumber,
                    Description = x.Description,
                    IsDefault = x.IsDefault,
                    ShowOnInvoices = x.ShowOnInvoices,
                    SortOrder = x.SortOrder
                })
                .ToListAsync();

            var customerBalance = await _context.CurrentAccountTransactions
                .AsNoTracking()
                .Where(x =>
                    x.WorkshopId == workshopId &&
                    x.CustomerId == invoice.CustomerId)
                .SumAsync(x => x.Debit - x.Credit);

            var invoicePaymentTotal = await _context.CurrentAccountTransactions
                .AsNoTracking()
                .Where(x =>
                    x.WorkshopId == workshopId &&
                    x.CustomerId == invoice.CustomerId &&
                    x.InvoiceId == invoice.Id &&
                    x.Type == CurrentAccountTransactionType.Payment)
                .SumAsync(x => x.Credit);

            var invoicePaymentCancelTotal = await _context.CurrentAccountTransactions
                .AsNoTracking()
                .Where(x =>
                    x.WorkshopId == workshopId &&
                    x.CustomerId == invoice.CustomerId &&
                    x.InvoiceId == invoice.Id &&
                    x.Type == CurrentAccountTransactionType.Cancel &&
                    x.Debit > 0 &&
                    x.DocumentNumber != null &&
                    x.DocumentNumber.StartsWith("PAY-CANCEL-"))
                .SumAsync(x => x.Debit);

            var invoicePaidTotal = Math.Max(0m, invoicePaymentTotal - invoicePaymentCancelTotal);

            var invoiceRemainingAmount = invoice.Status == InvoiceStatus.Issued
                ? Math.Max(0m, invoice.GrandTotal - invoicePaidTotal)
                : 0;

            var dto = new InvoiceDetailDto
            {
                Id = invoice.Id,
                WorkshopId = invoice.WorkshopId,
                CustomerId = invoice.CustomerId,
                ServiceRecordId = invoice.ServiceRecordId,

                Type = (int)invoice.Type,
                Status = (int)invoice.Status,

                InvoiceNumber = invoice.InvoiceNumber,
                InvoiceDate = invoice.InvoiceDate,

                CustomerTitle = invoice.CustomerTitle,
                CustomerTaxOffice = invoice.CustomerTaxOffice,
                CustomerTaxNumber = invoice.CustomerTaxNumber,
                CustomerTckn = invoice.CustomerTckn,
                CustomerAddress = invoice.CustomerAddress,

                Plate = invoice.Plate,
                ChassisNumber = invoice.ChassisNumber,
                Mileage = invoice.Mileage,
                VehicleBrandName = invoice.VehicleBrandName,
                VehicleModelName = invoice.VehicleModelName,
                VehicleModelYear = invoice.VehicleModelYear,
                WorkshopDisplayName = workshopProfile?.DisplayName,
                WorkshopLegalTitle = workshopProfile?.LegalTitle,
                WorkshopTaxOffice = workshopProfile?.TaxOffice,
                WorkshopTaxNumber = workshopProfile?.TaxNumber,
                WorkshopTradeRegistryNumber = workshopProfile?.TradeRegistryNumber,
                WorkshopMersisNumber = workshopProfile?.MersisNumber,
                WorkshopEmail = workshopProfile?.Email,
                WorkshopPhoneNumber = workshopProfile?.PhoneNumber,
                WorkshopFaxNumber = workshopProfile?.FaxNumber,
                WorkshopWebsite = workshopProfile?.Website,
                WorkshopAddressLine = workshopProfile?.AddressLine,
                WorkshopCity = workshopProfile?.City,
                WorkshopDistrict = workshopProfile?.District,
                WorkshopPostalCode = workshopProfile?.PostalCode,
                WorkshopCountry = workshopProfile?.Country,
                Subtotal = invoice.Subtotal,
                DiscountTotal = invoice.DiscountTotal,
                VatTotal = invoice.VatTotal,
                GrandTotal = invoice.GrandTotal,
                CustomerBalance = customerBalance,
                InvoicePaidTotal = invoicePaidTotal,
                InvoiceRemainingAmount = invoiceRemainingAmount,
                Notes = invoice.Notes,
                BankAccounts = bankAccounts
            };

            foreach (var item in invoice.Items)
            {
                dto.Items.Add(new InvoiceDetailItemDto
                {
                    Id = item.Id,
                    ItemType = (int)item.ItemType,

                    Description = item.Description,

                    Quantity = item.Quantity,
                    Unit = item.Unit,

                    UnitPrice = item.UnitPrice,

                    DiscountRate = item.DiscountRate,
                    DiscountAmount = item.DiscountAmount,

                    VatRate = item.VatRate,
                    VatAmount = item.VatAmount,

                    LineTotal = item.LineTotal,

                    StockItemId = item.StockItemId
                });
            }

            return ServiceResult<InvoiceDetailDto>.Success(dto);
        }


        public async Task<ServiceResult<IssueInvoiceResponseDto>> IssueAsync(int invoiceId, int workshopId)
        {
            var invoice = await _context.Invoices
                            .Include(x => x.Items)
                            .FirstOrDefaultAsync(x =>
                                x.Id == invoiceId &&
                                x.WorkshopId == workshopId);

            if (invoice is null)
                return ServiceResult<IssueInvoiceResponseDto>.Fail("Fatura bulunamadı.");

            if (invoice.Status == InvoiceStatus.Cancelled)
                return ServiceResult<IssueInvoiceResponseDto>.Fail("İptal edilmiş fatura kesilemez.");

            if (invoice.Status == InvoiceStatus.Issued)
                return ServiceResult<IssueInvoiceResponseDto>.Fail("Fatura zaten kesilmiş.");

            var oldStatus = invoice.Status;

            invoice.Status = InvoiceStatus.Issued;

            var shouldDecreaseStockOnIssue = !invoice.ServiceRecordId.HasValue;

            if (shouldDecreaseStockOnIssue)
            {
                foreach (var item in invoice.Items)
                {
                    if (item.StockItemId == null)
                        continue;

                    var stockResult = await _stockItemService.UseForInvoiceAsync(
                        item.StockItemId.Value,
                        item.Quantity,
                        item.UnitPrice,
                        invoice.Id,
                        workshopId);

                    if (!stockResult.IsSuccess)
                        return ServiceResult<IssueInvoiceResponseDto>.Fail(stockResult.ErrorMessage);
                }
            }


            var transaction = new CurrentAccountTransaction
            {
                WorkshopId = invoice.WorkshopId,
                CustomerId = invoice.CustomerId,
                InvoiceId = invoice.Id,

                Type = CurrentAccountTransactionType.InvoiceDebit,

                Debit = invoice.GrandTotal,
                Credit = 0,

                TransactionDate = _dateTimeProvider.Now,

                Description = $"{invoice.InvoiceNumber} numaralı fatura borç kaydı",

                DocumentNumber = invoice.InvoiceNumber,

                IsSystemGenerated = true
            };

            _context.CurrentAccountTransactions.Add(transaction);

            await _auditLogService.AddAsync(new AuditLogCreateDto
            {
                WorkshopId = workshopId,
                ActionType = AuditActionType.Issue,
                EntityType = AuditEntityType.Invoice,
                EntityId = invoice.Id,
                Description = $"Fatura kesildi: {GetInvoiceDisplayName(invoice)}",
                OldValues = new
                {
                    Status = oldStatus
                },
                NewValues = new
                {
                    invoice.Status,
                    invoice.GrandTotal,
                    CurrentAccountDebit = invoice.GrandTotal
                }
            });

            await _context.SaveChangesAsync();

            return ServiceResult<IssueInvoiceResponseDto>.Success(new IssueInvoiceResponseDto
            {
                InvoiceId = invoice.Id,
                Status = (int)invoice.Status,
                InvoiceNumber = invoice.InvoiceNumber
            });
        }

        public async Task<ServiceResult<List<InvoiceListItemDto>>> GetListAsync(int workshopId)
        {
            var invoices = await _context.Invoices
                .Where(x => x.WorkshopId == workshopId)
                .OrderByDescending(x => x.InvoiceDate)
                .Select(x => new InvoiceListItemDto
                {
                    Id = x.Id,
                    ServiceRecordId = x.ServiceRecordId,
                    Type = (int)x.Type,
                    Status = (int)x.Status,
                    InvoiceNumber = x.InvoiceNumber,
                    InvoiceDate = x.InvoiceDate,
                    CustomerTitle = x.CustomerTitle,
                    Plate = x.Plate,
                    GrandTotal = x.GrandTotal
                })
                .ToListAsync();

            return ServiceResult<List<InvoiceListItemDto>>.Success(invoices);
        }

        public async Task<ServiceResult<PagedResult<InvoiceListItemDto>>> GetPagedAsync(
    InvoiceListQueryDto query,
    int workshopId)
        {
            query ??= new InvoiceListQueryDto();
            query.Normalize();

            var invoicesQuery = _context.Invoices
                .AsNoTracking()
                .Where(x => x.WorkshopId == workshopId);

            if (query.Status.HasValue)
            {
                invoicesQuery = invoicesQuery.Where(x => x.Status == query.Status.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var searchText = query.Search.Trim();
                var search = $"%{searchText}%";
                var isNumericSearch = int.TryParse(searchText, out var numericSearch);

                invoicesQuery = invoicesQuery.Where(x =>
                    EF.Functions.Like(x.InvoiceNumber ?? string.Empty, search) ||
                    EF.Functions.Like(x.CustomerTitle ?? string.Empty, search) ||
                    EF.Functions.Like(x.Plate ?? string.Empty, search) ||
                    (isNumericSearch && x.Id == numericSearch) ||
                    (isNumericSearch && x.ServiceRecordId.HasValue && x.ServiceRecordId.Value == numericSearch));
            }

            var totalCount = await invoicesQuery.CountAsync();

            var items = await invoicesQuery
                .OrderByDescending(x => x.InvoiceDate)
                .ThenByDescending(x => x.Id)
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(x => new InvoiceListItemDto
                {
                    Id = x.Id,
                    ServiceRecordId = x.ServiceRecordId,
                    Type = (int)x.Type,
                    Status = (int)x.Status,
                    InvoiceNumber = x.InvoiceNumber,
                    InvoiceDate = x.InvoiceDate,
                    CustomerTitle = x.CustomerTitle,
                    Plate = x.Plate,
                    GrandTotal = x.GrandTotal
                })
                .ToListAsync();

            var pagedResult = new PagedResult<InvoiceListItemDto>
            {
                Items = items,
                PageNumber = query.PageNumber,
                PageSize = query.PageSize,
                TotalCount = totalCount
            };

            return ServiceResult<PagedResult<InvoiceListItemDto>>.Success(pagedResult);
        }

        public async Task<ServiceResult<List<InvoiceListItemDto>>> GetListByServiceRecordAsync(int serviceRecordId, int workshopId)
        {
            var invoices = await _context.Invoices
                .Where(x =>
                    x.WorkshopId == workshopId &&
                    x.ServiceRecordId == serviceRecordId)
                .OrderByDescending(x => x.InvoiceDate)
                .Select(x => new InvoiceListItemDto
                {
                    Id = x.Id,
                    ServiceRecordId = x.ServiceRecordId,
                    Type = (int)x.Type,
                    Status = (int)x.Status,
                    InvoiceNumber = x.InvoiceNumber,
                    InvoiceDate = x.InvoiceDate,
                    CustomerTitle = x.CustomerTitle,
                    Plate = x.Plate,
                    GrandTotal = x.GrandTotal
                })
                .ToListAsync();

            return ServiceResult<List<InvoiceListItemDto>>.Success(invoices);
        }
        public async Task<ServiceResult<InvoiceDetailDto>> GetDraftByServiceRecordAsync(int serviceRecordId, int workshopId)
        {
            var invoice = await _context.Invoices
                .Include(x => x.Items)
                .Where(x =>
                    x.WorkshopId == workshopId &&
                    x.ServiceRecordId == serviceRecordId &&
                    x.Status == InvoiceStatus.Draft)
                .OrderByDescending(x => x.InvoiceDate)
                .FirstOrDefaultAsync();

            if (invoice is null)
                return ServiceResult<InvoiceDetailDto>.Fail("Taslak fatura bulunamadı.");

            return await GetDetailAsync(invoice.Id, workshopId);
        }

        public async Task<ServiceResult<bool>> SyncDraftByServiceRecordAsync(
    int serviceRecordId,
    int workshopId)
        {
            var invoice = await _context.Invoices
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x =>
                    x.WorkshopId == workshopId &&
                    x.ServiceRecordId == serviceRecordId &&
                    x.Status == InvoiceStatus.Draft);

            if (invoice is null)
                return ServiceResult<bool>.Success(true);

            var serviceRecordExists = await _context.ServiceRecords
                .AnyAsync(x =>
                    x.Id == serviceRecordId &&
                    x.WorkshopId == workshopId);

            if (!serviceRecordExists)
                return ServiceResult<bool>.Fail("Servis kaydı bulunamadı.");

            var operations = await _context.ServiceOperations
                .Where(x =>
                    x.ServiceRecordId == serviceRecordId &&
                    !x.IsDeleted)
                .OrderBy(x => x.Id)
                .ToListAsync();

            _context.InvoiceItems.RemoveRange(invoice.Items);

            decimal subtotal = 0;
            decimal discountTotal = 0;
            decimal vatTotal = 0;
            decimal grandTotal = 0;

            foreach (var operation in operations)
            {
                var quantity = operation.Quantity;
                var unitPrice = operation.UnitPrice;
                var discountRate = 0m;
                var vatRate = 20m;

                var lineSubtotal = quantity * unitPrice;
                var discountAmount = lineSubtotal * discountRate / 100;
                var taxableAmount = lineSubtotal - discountAmount;
                var vatAmount = taxableAmount * vatRate / 100;
                var lineTotal = taxableAmount + vatAmount;

                subtotal += lineSubtotal;
                discountTotal += discountAmount;
                vatTotal += vatAmount;
                grandTotal += lineTotal;

                invoice.Items.Add(new InvoiceItem
                {
                    ItemType = operation.Type == OperationType.Part
                        ? InvoiceItemType.Part
                        : InvoiceItemType.Labor,

                    Description = operation.Description.Trim(),
                    Quantity = quantity,
                    Unit = "Adet",
                    UnitPrice = unitPrice,
                    DiscountRate = discountRate,
                    DiscountAmount = discountAmount,
                    VatRate = vatRate,
                    VatAmount = vatAmount,
                    LineTotal = lineTotal,
                    StockItemId = operation.StockItemId
                });
            }

            invoice.Subtotal = subtotal;
            invoice.DiscountTotal = discountTotal;
            invoice.VatTotal = vatTotal;
            invoice.GrandTotal = grandTotal;

            await _context.SaveChangesAsync();

            return ServiceResult<bool>.Success(true);
        }

        public async Task<ServiceResult<InvoiceNavigationDto>> GetActiveInvoiceByServiceRecordAsync(int serviceRecordId, int workshopId)
        {
            var invoice = await _context.Invoices
                .Where(x =>
                    x.WorkshopId == workshopId &&
                    x.ServiceRecordId == serviceRecordId &&
                    x.Status != InvoiceStatus.Cancelled)
                .OrderBy(x => x.Status == InvoiceStatus.Draft ? 0 : 1)
                .ThenByDescending(x => x.InvoiceDate)
                .FirstOrDefaultAsync();

            if (invoice is null)
                return ServiceResult<InvoiceNavigationDto>.Fail("Aktif fatura bulunamadı.");

            return ServiceResult<InvoiceNavigationDto>.Success(new InvoiceNavigationDto
            {
                InvoiceId = invoice.Id,
                Status = (int)invoice.Status,
                InvoiceNumber = invoice.InvoiceNumber
            });
        }

        public async Task<ServiceResult<CancelInvoiceResponseDto>> CancelAsync(int invoiceId, int workshopId)
        {
            var invoice = await _context.Invoices
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x =>
                    x.Id == invoiceId &&
                    x.WorkshopId == workshopId);

            if (invoice is null)
                return ServiceResult<CancelInvoiceResponseDto>.Fail("Fatura bulunamadı.");

            if (invoice.Status == InvoiceStatus.Cancelled)
                return ServiceResult<CancelInvoiceResponseDto>.Fail("Fatura zaten iptal edilmiş.");

            var oldStatus = invoice.Status;
            var wasIssued = oldStatus == InvoiceStatus.Issued;
            var shouldReturnStockOnCancel = wasIssued && !invoice.ServiceRecordId.HasValue;

            if (wasIssued)
            {
                var hasCancelTransaction = await _context.CurrentAccountTransactions
                    .AnyAsync(x =>
                        x.WorkshopId == workshopId &&
                        x.InvoiceId == invoice.Id &&
                        x.Type == CurrentAccountTransactionType.Cancel);

                if (hasCancelTransaction)
                    return ServiceResult<CancelInvoiceResponseDto>.Fail("Bu fatura için iptal hesap hareketi zaten oluşturulmuş.");
            }

            await using var dbTransaction = await _context.Database.BeginTransactionAsync();

            invoice.Status = InvoiceStatus.Cancelled;

            if (shouldReturnStockOnCancel)
            {
                foreach (var item in invoice.Items)
                {
                    if (item.StockItemId is null)
                        continue;

                    var stockResult = await _stockItemService.ReturnForInvoiceCancellationAsync(
                        item.StockItemId.Value,
                        item.Quantity,
                        item.UnitPrice,
                        invoice.Id,
                        workshopId);

                    if (!stockResult.IsSuccess)
                    {
                        await dbTransaction.RollbackAsync();

                        return ServiceResult<CancelInvoiceResponseDto>.Fail(
                            stockResult.ErrorMessage ?? "Fatura iptalinde stok iadesi yapılamadı.");
                    }
                }
            }

            if (wasIssued)
            {
                var currentAccountTransaction = new CurrentAccountTransaction
                {
                    WorkshopId = invoice.WorkshopId,
                    CustomerId = invoice.CustomerId,
                    InvoiceId = invoice.Id,

                    Type = CurrentAccountTransactionType.Cancel,

                    Debit = 0,
                    Credit = invoice.GrandTotal,

                    TransactionDate = _dateTimeProvider.Now,

                    Description = $"{invoice.InvoiceNumber} numaralı fatura iptal kaydı",

                    DocumentNumber = invoice.InvoiceNumber,

                    IsSystemGenerated = true
                };

                _context.CurrentAccountTransactions.Add(currentAccountTransaction);
            }

            await _auditLogService.AddAsync(new AuditLogCreateDto
            {
                WorkshopId = workshopId,
                ActionType = AuditActionType.Cancel,
                EntityType = AuditEntityType.Invoice,
                EntityId = invoice.Id,
                Description = $"Fatura iptal edildi: {GetInvoiceDisplayName(invoice)}",
                OldValues = new
                {
                    Status = oldStatus
                },
                NewValues = new
                {
                    invoice.Status,
                    invoice.GrandTotal,
                    CurrentAccountCredit = wasIssued ? invoice.GrandTotal : 0,
                    StockReturned = shouldReturnStockOnCancel
                }
            });

            await _context.SaveChangesAsync();

            await dbTransaction.CommitAsync();

            return ServiceResult<CancelInvoiceResponseDto>.Success(
                new CancelInvoiceResponseDto
                {
                    InvoiceId = invoice.Id,
                    Status = (int)invoice.Status
                });
        }

        public async Task<ServiceResult<InvoiceDetailDto>> UpdateAsync(UpdateInvoiceDto request, int workshopId)
        {
            var invoice = await _context.Invoices
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x =>
                    x.Id == request.InvoiceId &&
                    x.WorkshopId == workshopId);

            if (invoice is null)
                return ServiceResult<InvoiceDetailDto>.Fail("Fatura bulunamadı.");

            if (invoice.Status != InvoiceStatus.Draft)
                return ServiceResult<InvoiceDetailDto>.Fail("Sadece taslak faturalar düzenlenebilir.");

            var oldValues = GetInvoiceAuditValues(invoice);

            var validItems = request.Items
                .Where(x =>
                    !string.IsNullOrWhiteSpace(x.Description) &&
                    x.Quantity > 0)
                .ToList();

            var stockItemIds = validItems
                    .Where(x => x.StockItemId.HasValue)
                    .Select(x => x.StockItemId!.Value)
                    .Distinct()
                    .ToList();

            if (stockItemIds.Any())
            {
                var validStockItemCount = await _context.StockItems
                    .CountAsync(x =>
                        stockItemIds.Contains(x.Id) &&
                        x.WorkshopId == workshopId &&
                        x.IsActive);

                if (validStockItemCount != stockItemIds.Count)
                    return ServiceResult<InvoiceDetailDto>.Fail(
                    "Fatura kalemlerinde geçersiz stok kartı seçimi var.");
            }


            if (!validItems.Any())
                return ServiceResult<InvoiceDetailDto>.Fail("Geçerli fatura kalemi bulunamadı.");

            invoice.CustomerTitle = string.IsNullOrWhiteSpace(request.CustomerTitle)
                ? invoice.CustomerTitle
                : request.CustomerTitle.Trim();

            invoice.CustomerTaxOffice = request.CustomerTaxOffice;
            invoice.CustomerTaxNumber = request.CustomerTaxNumber;
            invoice.CustomerTckn = request.CustomerTckn;
            invoice.CustomerAddress = request.CustomerAddress;

            invoice.Plate = request.Plate;
            invoice.ChassisNumber = request.ChassisNumber;
            invoice.VehicleBrandName = request.VehicleBrandName;
            invoice.VehicleModelName = request.VehicleModelName;
            invoice.VehicleModelYear = request.VehicleModelYear;
            invoice.Mileage = request.Mileage;

            invoice.Notes = request.Notes;

            var customerCardUpdateResult = await UpdateCustomerCardIfRequestedAsync(
                invoice,
                request,
                workshopId);

            if (customerCardUpdateResult.IsFailure)
            {
                return ServiceResult<InvoiceDetailDto>.Fail(
                    customerCardUpdateResult.ErrorMessage ?? "Müşteri kaydı güncellenemedi.");
            }

            var vehicleCardUpdateResult = await UpdateVehicleCardIfRequestedAsync(
                invoice,
                request,
                workshopId);

            if (vehicleCardUpdateResult.IsFailure)
            {
                return ServiceResult<InvoiceDetailDto>.Fail(
                    vehicleCardUpdateResult.ErrorMessage ?? "Araç kaydı güncellenemedi.");
            }

            _context.InvoiceItems.RemoveRange(invoice.Items);

            decimal subtotal = 0;
            decimal discountTotal = 0;
            decimal vatTotal = 0;
            decimal grandTotal = 0;

            foreach (var item in validItems)
            {
                var quantity = item.Quantity;
                var unitPrice = item.UnitPrice;

                var discountRate = item.DiscountRate < 0 ? 0 : item.DiscountRate;
                var vatRate = item.VatRate < 0 ? 0 : item.VatRate;

                var lineSubtotal = quantity * unitPrice;
                var discountAmount = lineSubtotal * discountRate / 100;
                var taxableAmount = lineSubtotal - discountAmount;
                var vatAmount = taxableAmount * vatRate / 100;
                var lineTotal = taxableAmount + vatAmount;

                subtotal += lineSubtotal;
                discountTotal += discountAmount;
                vatTotal += vatAmount;
                grandTotal += lineTotal;

                invoice.Items.Add(new InvoiceItem
                {
                    ItemType = (InvoiceItemType)item.ItemType,

                    Description = item.Description!.Trim(),

                    Quantity = quantity,

                    Unit = string.IsNullOrWhiteSpace(item.Unit) ? "Adet"  : item.Unit.Trim(),

                    UnitPrice = unitPrice,

                    DiscountRate = discountRate,
                    DiscountAmount = discountAmount,

                    VatRate = vatRate,
                    VatAmount = vatAmount,

                    LineTotal = lineTotal,

                    StockItemId = item.StockItemId
                });
            }

            invoice.Subtotal = subtotal;
            invoice.DiscountTotal = discountTotal;
            invoice.VatTotal = vatTotal;
            invoice.GrandTotal = grandTotal;

            await _auditLogService.AddAsync(new AuditLogCreateDto
            {
                WorkshopId = workshopId,
                ActionType = AuditActionType.Update,
                EntityType = AuditEntityType.Invoice,
                EntityId = invoice.Id,
                Description = $"Fatura güncellendi: {GetInvoiceDisplayName(invoice)}",
                OldValues = oldValues,
                NewValues = GetInvoiceAuditValues(invoice)
            });

            await _context.SaveChangesAsync();

            return await GetDetailAsync(invoice.Id, workshopId);
        }

        public async Task<ServiceResult<InvoiceNavigationDto>> CreateOrGetDraftFromServiceRecordAsync(
    int serviceRecordId,
    int workshopId)
        {
            var activeInvoice = await _context.Invoices
                .Where(x =>
                    x.WorkshopId == workshopId &&
                    x.ServiceRecordId == serviceRecordId &&
                    x.Status != InvoiceStatus.Cancelled)
                .OrderBy(x => x.Status == InvoiceStatus.Draft ? 0 : 1)
                .ThenByDescending(x => x.InvoiceDate)
                .FirstOrDefaultAsync();

            if (activeInvoice is not null)
            {
                return ServiceResult<InvoiceNavigationDto>.Success(new InvoiceNavigationDto
                {
                    InvoiceId = activeInvoice.Id,
                    Status = (int)activeInvoice.Status,
                    InvoiceNumber = activeInvoice.InvoiceNumber
                });
            }

            var draftResult = await GetCreateDraftAsync(serviceRecordId, workshopId);

            if (!draftResult.IsSuccess || draftResult.Data is null)
            {
                return ServiceResult<InvoiceNavigationDto>.Fail(
                    draftResult.ErrorMessage ?? "Fatura taslağı hazırlanamadı.");
            }

            var draft = draftResult.Data;

            if (draft.Items is null || !draft.Items.Any())
            {
                return ServiceResult<InvoiceNavigationDto>.Fail(
                    "Fatura oluşturmak için servis kaydında en az bir işlem olmalıdır.");
            }

            var createRequest = new CreateInvoiceDto
            {
                ServiceRecordId = draft.ServiceRecordId,
                CustomerId = draft.CustomerId,
                InvoiceType = 1,

                CustomerTitle = draft.CustomerTitle,
                CustomerTaxOffice = draft.CustomerTaxOffice,
                CustomerTaxNumber = draft.CustomerTaxNumber,
                CustomerTckn = draft.CustomerTckn,
                CustomerAddress = draft.CustomerAddress,

                Plate = draft.Plate,
                ChassisNumber = draft.ChassisNumber,
                Mileage = draft.Mileage,
                VehicleBrandName = draft.VehicleBrandName,
                VehicleModelName = draft.VehicleModelName,
                VehicleModelYear = draft.VehicleModelYear,

                Items = draft.Items.Select(x => new CreateInvoiceItemDto
                {
                    ItemType = x.ItemType,
                    Description = x.Description,
                    Quantity = x.Quantity,
                    Unit = x.Unit,
                    UnitPrice = x.UnitPrice,
                    DiscountRate = x.DiscountRate,
                    VatRate = x.VatRate,
                    StockItemId = x.StockItemId
                }).ToList()
            };

            var createResult = await CreateAsync(createRequest, workshopId);

            if (!createResult.IsSuccess || createResult.Data is null)
            {
                return ServiceResult<InvoiceNavigationDto>.Fail(
                    createResult.ErrorMessage ?? "Fatura oluşturulamadı.");
            }

            return ServiceResult<InvoiceNavigationDto>.Success(new InvoiceNavigationDto
            {
                InvoiceId = createResult.Data.InvoiceId,
                Status = 1,
                InvoiceNumber = createResult.Data.InvoiceNumber ?? string.Empty
            });
        }

        private static string GetInvoiceDisplayName(Invoice invoice)
        {
            if (!string.IsNullOrWhiteSpace(invoice.InvoiceNumber))
                return $"{invoice.InvoiceNumber} / {invoice.CustomerTitle}";

            return $"Fatura #{invoice.Id} / {invoice.CustomerTitle}";
        }

        private static object GetInvoiceAuditValues(Invoice invoice)
        {
            return new
            {
                invoice.InvoiceNumber,
                invoice.Status,
                invoice.ServiceRecordId,
                invoice.CustomerId,
                invoice.CustomerTitle,
                invoice.Plate,
                invoice.Subtotal,
                invoice.DiscountTotal,
                invoice.VatTotal,
                invoice.GrandTotal,
                ItemCount = invoice.Items?.Count ?? 0
            };
        }
        private async Task<ServiceResult<bool>> UpdateCustomerCardIfRequestedAsync(
    Invoice invoice,
    UpdateInvoiceDto request,
    int workshopId)
        {
            if (!request.UpdateCustomerCard)
                return ServiceResult<bool>.Success(true);

            var customer = await _context.Customers
                .FirstOrDefaultAsync(x =>
                    x.Id == invoice.CustomerId &&
                    x.WorkshopId == workshopId);

            if (customer is null)
                return ServiceResult<bool>.Fail("Müşteri kaydı bulunamadı.");

            var customerType = ResolveInvoiceCustomerType(
                request.CustomerType,
                request);

            if (customerType is null)
                return ServiceResult<bool>.Fail("Müşteri tipi hatalı.");

            var customerTitle = NormalizeNullable(request.CustomerTitle);

            if (!string.IsNullOrWhiteSpace(customerTitle))
                customer.FullName = customerTitle;

            customer.Address = NormalizeNullable(request.CustomerAddress);
            customer.Type = customerType.Value;

            if (customerType.Value == CustomerType.Individual)
            {
                customer.CompanyName = null;
                customer.NationalIdentityNumber = NormalizeNullable(request.CustomerTckn);
                customer.TaxOffice = null;
                customer.TaxNumber = null;
            }
            else if (customerType.Value == CustomerType.SoleProprietorship)
            {
                customer.TaxOffice = NormalizeNullable(request.CustomerTaxOffice);
                customer.TaxNumber = NormalizeNullable(request.CustomerTaxNumber);
                customer.NationalIdentityNumber = null;
            }
            else if (customerType.Value == CustomerType.Corporate)
            {
                customer.TaxOffice = NormalizeNullable(request.CustomerTaxOffice);
                customer.TaxNumber = NormalizeNullable(request.CustomerTaxNumber);
                customer.NationalIdentityNumber = null;
            }

            return ServiceResult<bool>.Success(true);
        }

        private async Task<ServiceResult<bool>> UpdateVehicleCardIfRequestedAsync(
            Invoice invoice,
            UpdateInvoiceDto request,
            int workshopId)
        {
            if (!request.UpdateVehicleCard)
                return ServiceResult<bool>.Success(true);

            if (!invoice.ServiceRecordId.HasValue)
            {
                return ServiceResult<bool>.Fail(
                    "Araç kaydını güncellemek için faturanın servis kaydıyla bağlantılı olması gerekir.");
            }

            var serviceRecord = await _context.ServiceRecords
                .FirstOrDefaultAsync(x =>
                    x.Id == invoice.ServiceRecordId.Value &&
                    x.WorkshopId == workshopId);

            if (serviceRecord is null)
                return ServiceResult<bool>.Fail("Servis kaydı bulunamadı.");

            var vehicle = await _context.Vehicles
                .FirstOrDefaultAsync(x =>
                    x.Id == serviceRecord.VehicleId &&
                    x.WorkshopId == workshopId);

            if (vehicle is null)
                return ServiceResult<bool>.Fail("Araç kaydı bulunamadı.");

            var plate = NormalizePlate(request.Plate);

            if (!string.IsNullOrWhiteSpace(plate))
            {
                vehicle.Plate = plate;
                serviceRecord.VehiclePlateSnapshot = plate;
            }

            vehicle.ChassisNumber = NormalizeNullable(request.ChassisNumber);

            serviceRecord.MileageSnapshot = request.Mileage;
            invoice.Mileage = request.Mileage;

            return ServiceResult<bool>.Success(true);
        }

        private static CustomerType? ResolveInvoiceCustomerType(
            string? customerType,
            UpdateInvoiceDto request)
        {
            var normalized = customerType?.Trim().ToLowerInvariant();

            return normalized switch
            {
                "individual" => CustomerType.Individual,
                "bireysel" => CustomerType.Individual,

                "sole" => CustomerType.SoleProprietorship,
                "soleproprietorship" => CustomerType.SoleProprietorship,
                "sahis" => CustomerType.SoleProprietorship,
                "şahıs" => CustomerType.SoleProprietorship,

                "corporate" => CustomerType.Corporate,
                "kurumsal" => CustomerType.Corporate,

                _ when !string.IsNullOrWhiteSpace(request.CustomerTckn)
                    => CustomerType.Individual,

                _ when !string.IsNullOrWhiteSpace(request.CustomerTaxNumber) ||
                       !string.IsNullOrWhiteSpace(request.CustomerTaxOffice)
                    => CustomerType.SoleProprietorship,

                _ => CustomerType.Individual
            };
        }

        private static string? NormalizeNullable(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }

        private static string? NormalizePlate(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            return value
                .Trim()
                .Replace(" ", "")
                .ToUpperInvariant();
        }
    }
}
