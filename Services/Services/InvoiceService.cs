using AutoStock.Repositories;
using AutoStock.Repositories.Entities;
using AutoStock.Repositories.Enums;
using AutoStock.Services.Dtos.Common;
using AutoStock.Services.Dtos.Invoices;
using AutoStock.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AutoStock.Services.Services
{
    public class InvoiceService : IInvoiceService
    {
        private readonly AppDbContext _context;

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

                InvoiceNumber = $"MAT-{DateTime.UtcNow:yyyyMMddHHmmss}",
                InvoiceDate = DateTime.UtcNow,

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

                CreatedAt = DateTime.UtcNow
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

                    LineTotal = lineTotal
                });
            }

            invoice.Subtotal = subTotal;
            invoice.DiscountTotal = discountTotal;
            invoice.VatTotal = vatTotal;
            invoice.GrandTotal = grandTotal;

            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();

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
                .FirstOrDefaultAsync(x =>
                    x.Id == invoiceId &&
                    x.WorkshopId == workshopId);

            if (invoice is null)
                return ServiceResult<IssueInvoiceResponseDto>.Fail("Fatura bulunamadı.");

            if (invoice.Status == InvoiceStatus.Cancelled)
                return ServiceResult<IssueInvoiceResponseDto>.Fail("İptal edilmiş fatura kesilemez.");

            if (invoice.Status == InvoiceStatus.Issued)
                return ServiceResult<IssueInvoiceResponseDto>.Fail("Fatura zaten kesilmiş.");

            invoice.Status = InvoiceStatus.Issued;

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

            invoice.Status = InvoiceStatus.Cancelled;

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
                    Unit = string.IsNullOrWhiteSpace(item.Unit) ? "Adet" : item.Unit.Trim(),
                    UnitPrice = unitPrice,
                    DiscountRate = discountRate,
                    DiscountAmount = discountAmount,
                    VatRate = vatRate,
                    VatAmount = vatAmount,
                    LineTotal = lineTotal
                });
            }

            invoice.Subtotal = subtotal;
            invoice.DiscountTotal = discountTotal;
            invoice.VatTotal = vatTotal;
            invoice.GrandTotal = grandTotal;

            await _context.SaveChangesAsync();

            return await GetDetailAsync(invoice.Id, workshopId);
        }
    }
}