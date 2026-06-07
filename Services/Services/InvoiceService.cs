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

        public InvoiceService(AppDbContext context)
        {
            _context = context;
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
                Mileage = serviceRecord.MileageSnapshot
            };

            foreach (var operation in serviceRecord.Operations)
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
                CustomerId = request.CustomerId,

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

            var customerBalance = await _context.CurrentAccountTransactions
    .Where(x =>
        x.WorkshopId == workshopId &&
        x.CustomerId == invoice.CustomerId)
    .SumAsync(x => x.Debit - x.Credit);

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

                Subtotal = invoice.Subtotal,
                DiscountTotal = invoice.DiscountTotal,
                VatTotal = invoice.VatTotal,
                GrandTotal = invoice.GrandTotal,
                CustomerBalance = customerBalance,
                Notes = invoice.Notes
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

                    LineTotal = item.LineTotal
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
                .FirstOrDefaultAsync(x =>
                    x.Id == invoiceId &&
                    x.WorkshopId == workshopId);

            if (invoice is null)
                return ServiceResult<CancelInvoiceResponseDto>.Fail("Fatura bulunamadı.");

            if (invoice.Status == InvoiceStatus.Cancelled)
                return ServiceResult<CancelInvoiceResponseDto>.Fail("Fatura zaten iptal edilmiş.");

            var hasCancelTransaction = await _context.CurrentAccountTransactions
    .AnyAsync(x =>
        x.InvoiceId == invoice.Id &&
        x.Type == CurrentAccountTransactionType.Cancel);

            if (hasCancelTransaction)
                return ServiceResult<CancelInvoiceResponseDto>.Fail("Bu fatura için iptal cari hareketi zaten oluşturulmuş.");

            var oldStatus = invoice.Status;

            invoice.Status = InvoiceStatus.Cancelled;

            var transaction = new CurrentAccountTransaction
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

            _context.CurrentAccountTransactions.Add(transaction);

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
                    CurrentAccountCredit = invoice.GrandTotal
                }
            });

            await _context.SaveChangesAsync();

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
            invoice.Mileage = request.Mileage;

            invoice.Notes = request.Notes;

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
    }
}