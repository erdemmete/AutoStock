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

        public async Task<ServiceResult<CreateInvoiceDraftDto>> GetCreateDraftAsync(
            int serviceRecordId,
            int workshopId)
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

        public async Task<ServiceResult<CreateInvoiceResponseDto>> CreateAsync(
    CreateInvoiceDto request,
    int workshopId)
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
    }
}