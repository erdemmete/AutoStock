using AutoStock.Repositories;
using AutoStock.Repositories.Entities;
using AutoStock.Repositories.Enums;
using AutoStock.Services.Dtos.Pdfs;
using AutoStock.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoStockAPI.Controllers
{
    [ApiController]
    [Route("public/service-pdf")]
    public class PublicServicePdfController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IPdfService _pdfService;

        public PublicServicePdfController(
            AppDbContext context,
            IPdfService pdfService)
        {
            _context = context;
            _pdfService = pdfService;
        }

        [HttpGet("{qrCode}/{serviceRecordId:int}")]
        public async Task<IActionResult> Get(string qrCode, int serviceRecordId)
        {
            if (string.IsNullOrWhiteSpace(qrCode))
                return BadRequest("QR kod zorunludur.");

            var qr = await _context.VehicleQrCodes
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Code == qrCode);

            if (qr is null || !qr.VehicleId.HasValue)
                return NotFound("QR kod bulunamadı veya araca atanmamış.");

            var record = await _context.ServiceRecords
                .Include(x => x.Vehicle)
                    .ThenInclude(x => x.VehicleBrand)
                .Include(x => x.Vehicle)
                    .ThenInclude(x => x.VehicleModel)
                .Include(x => x.Vehicle)
                    .ThenInclude(x => x.VehicleVariant)
                .Include(x => x.Operations)
                .Include(x => x.RequestItems)
                .FirstOrDefaultAsync(x =>
                    x.Id == serviceRecordId &&
                    x.VehicleId == qr.VehicleId.Value);

            if (record is null)
                return NotFound("Servis kaydı bulunamadı.");

            var workshop = await _context.Workshops
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == record.WorkshopId);

            var workshopProfile = await _context.WorkshopProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.WorkshopId == record.WorkshopId);

            var workshopName = !string.IsNullOrWhiteSpace(workshopProfile?.DisplayName)
                ? workshopProfile.DisplayName
                : workshop?.Name;

            var workshopAddress = BuildAddress(
                workshopProfile?.AddressLine,
                workshopProfile?.District,
                workshopProfile?.City);

            var bankAccounts = await GetServiceFormBankAccountsAsync(record.WorkshopId);

            var pdfRequest = new CreateServicePdfRequest
            {
                WorkshopName = workshopName ?? "Sente360",
                WorkshopAddress = workshopAddress,
                WorkshopPhone = workshopProfile?.PhoneNumber,

                RecordNumber = record.RecordNumber,

                StatusText = ToStatusText(record.Status),

                CustomerName = record.CustomerNameSnapshot,
                CustomerPhone = record.CustomerPhoneSnapshot,
                CustomerEmail = null,

                Plate = record.Vehicle?.Plate
                    ?? record.VehiclePlateSnapshot,

                Brand = record.Vehicle?.VehicleBrand?.Name
                    ?? record.VehicleBrandNameSnapshot,

                Model = record.Vehicle?.VehicleModel?.Name
                    ?? record.VehicleModelNameSnapshot,

                ModelYear = record.Vehicle?.ModelYear?.ToString(),

                FuelLevelText = ToFuelLevelText(record.FuelLevelSnapshot),

                VehicleVariantName = record.Vehicle?.VehicleVariant?.Name,
                FuelType = record.Vehicle?.FuelType,
                TransmissionType = record.Vehicle?.TransmissionType,
                BodyType = record.Vehicle?.BodyType,
                EngineCapacityCc = record.Vehicle?.EngineCapacityCc,
                EnginePowerHp = record.Vehicle?.EnginePowerHp,
                EngineCode = record.Vehicle?.EngineCode,

                Note = record.ServiceReceptionNote,

                BankAccounts = bankAccounts,

                // ÖNEMLİ:
                // QR/public PDF'de sadece müşteri adı ve plaka maskelenir.
                // İşlemler, fiyatlar, araç marka/model, servis bilgileri ve IBAN görünür.
                IsPublicMasked = true,

                RequestGroups = record.RequestItems
                    .Where(x => !x.IsDeleted)
                    .OrderBy(x => x.Id)
                    .Select(item => new ServicePdfRequestGroupDto
                    {
                        Id = item.Id,
                        Title = item.Title,
                        Note = item.Note,
                        EstimatedAmount = item.EstimatedAmount,

                        Operations = record.Operations
                            .Where(op =>
                                !op.IsDeleted &&
                                op.ServiceRequestItemId == item.Id)
                            .OrderBy(op => op.Id)
                            .Select(op => new ServicePdfItemDto
                            {
                                TypeText = op.Type == OperationType.Part
                                    ? "Parça"
                                    : "İşçilik",

                                Name = op.Description,
                                Quantity = op.Quantity,
                                UnitPrice = op.UnitPrice,
                                TotalPrice = op.TotalPrice,
                                Note = op.Note
                            })
                            .ToList()
                    })
                    .ToList()
            };

            var pdfBytes = _pdfService.CreateServicePdf(pdfRequest);

            var fileName = $"{record.RecordNumber}-skf.pdf";

            return File(pdfBytes, "application/pdf", fileName);
        }

        private async Task<List<ServicePdfBankAccountDto>> GetServiceFormBankAccountsAsync(int workshopId)
        {
            return await _context.Set<WorkshopBankAccount>()
                .AsNoTracking()
                .Where(x =>
                    x.WorkshopId == workshopId &&
                    x.IsActive &&
                    x.ShowOnServiceForms)
                .OrderByDescending(x => x.IsDefault)
                .ThenBy(x => x.SortOrder)
                .ThenBy(x => x.BankName)
                .Select(x => new ServicePdfBankAccountDto
                {
                    Id = x.Id,
                    BankName = x.BankName,
                    AccountHolder = x.AccountHolder,
                    Iban = x.Iban,
                    CurrencyCode = x.CurrencyCode,
                    Description = x.Description,
                    IsDefault = x.IsDefault,
                    SortOrder = x.SortOrder
                })
                .ToListAsync();
        }

        private static string? BuildAddress(params string?[] parts)
        {
            var values = parts
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!.Trim());

            var address = string.Join(" / ", values);

            return string.IsNullOrWhiteSpace(address)
                ? null
                : address;
        }

        private static string ToStatusText(ServiceRecordStatus status)
        {
            return status switch
            {
                ServiceRecordStatus.Open => "Açık Kayıt",
                ServiceRecordStatus.InProgress => "İşlemde",
                ServiceRecordStatus.Completed => "Tamamlandı",
                ServiceRecordStatus.Cancelled => "İptal Edildi",
                _ => "Bilinmiyor"
            };
        }

        private static string ToFuelLevelText(FuelLevel? fuelLevel)
        {
            return fuelLevel switch
            {
                FuelLevel.Empty => "Boş",
                FuelLevel.Quarter => "1/4",
                FuelLevel.Half => "1/2",
                FuelLevel.ThreeQuarters => "3/4",
                FuelLevel.Full => "Dolu",
                _ => "-"
            };
        }
    }
}