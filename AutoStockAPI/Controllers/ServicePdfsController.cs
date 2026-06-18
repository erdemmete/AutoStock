using AutoStock.Repositories;
using AutoStock.Repositories.Entities;
using AutoStock.Repositories.Enums;
using AutoStock.Services.Constants;
using AutoStock.Services.Dtos.Pdfs;
using AutoStock.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;

namespace AutoStock.API.Controllers
{
    [Authorize(Roles = AppRoles.Owner + "," + AppRoles.Staff)]
    [Route("api/[controller]")]
    [ApiController]
    public class ServicePdfsController : BaseApiController
    {
        private readonly IPdfService _pdfService;
        private readonly AppDbContext _context;

        public ServicePdfsController(
            IPdfService pdfService,
            AppDbContext context)
        {
            _pdfService = pdfService;
            _context = context;
        }

        [HttpGet("{serviceRecordId:int}")]
        public async Task<IActionResult> Create(int serviceRecordId, [FromQuery] string? publicBaseUrl)
        {
            var workshopIdResult = GetCurrentWorkshopId();

            if (workshopIdResult.IsFailure)
                return UnauthorizedResult(workshopIdResult);

            var workshopId = workshopIdResult.Data;

            var serviceRecord = await _context.ServiceRecords
                .Include(x => x.Customer)
                .Include(x => x.Vehicle)
                    .ThenInclude(x => x.VehicleBrand)
                .Include(x => x.Vehicle)
                    .ThenInclude(x => x.VehicleModel)
                .Include(x => x.Vehicle)
                    .ThenInclude(x => x.VehicleVariant)
                .Include(x => x.RequestItems)
                .Include(x => x.Operations)
                .FirstOrDefaultAsync(x =>
                    x.Id == serviceRecordId &&
                    x.WorkshopId == workshopId);

            if (serviceRecord is null)
                return NotFound("Servis kaydı bulunamadı.");

            var workshop = await _context.Workshops
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == workshopId);

            var workshopProfile = await _context.WorkshopProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.WorkshopId == workshopId);

            var workshopName = !string.IsNullOrWhiteSpace(workshopProfile?.DisplayName)
                ? workshopProfile.DisplayName
                : workshop?.Name;

            var workshopAddress = BuildAddress(
                workshopProfile?.AddressLine,
                workshopProfile?.District,
                workshopProfile?.City);

            var bankAccounts = await GetServiceFormBankAccountsAsync(workshopId);
            var activeQrCode = await _context.VehicleQrCodes
                .AsNoTracking()
                .Where(x =>
                    x.WorkshopId == workshopId &&
                    x.VehicleId == serviceRecord.VehicleId &&
                    x.Status == VehicleQrCodeStatus.Assigned)
                .OrderByDescending(x => x.AssignedAt)
                .Select(x => x.Code)
                .FirstOrDefaultAsync();

            var vehicleQrPublicUrl = !string.IsNullOrWhiteSpace(activeQrCode)
                ? BuildPublicQrUrl(activeQrCode, publicBaseUrl)
                : null;

            var request = new CreateServicePdfRequest
            {
                WorkshopName = workshopName ?? "Servis adı belirtilmedi",
                WorkshopAddress = workshopAddress,
                WorkshopPhone = workshopProfile?.PhoneNumber,

                RecordNumber = serviceRecord.RecordNumber,

                StatusText = ToStatusText(serviceRecord.Status),

                CustomerName = serviceRecord.CustomerNameSnapshot
                    ?? serviceRecord.Customer?.FullName,

                CustomerPhone = serviceRecord.CustomerPhoneSnapshot
                    ?? serviceRecord.Customer?.PhoneNumber,

                CustomerEmail = serviceRecord.Customer?.Email,

                Plate = serviceRecord.Vehicle?.Plate
                    ?? serviceRecord.VehiclePlateSnapshot,

                Brand = serviceRecord.Vehicle?.VehicleBrand?.Name
                    ?? serviceRecord.VehicleBrandNameSnapshot,

                Model = serviceRecord.Vehicle?.VehicleModel?.Name
                    ?? serviceRecord.VehicleModelNameSnapshot,

                ModelYear = serviceRecord.Vehicle?.ModelYear?.ToString(),

                FuelLevelText = ToFuelLevelText(serviceRecord.FuelLevelSnapshot),

                VehicleVariantName = serviceRecord.Vehicle?.VehicleVariant?.Name,
                FuelType = serviceRecord.Vehicle?.FuelType,
                TransmissionType = serviceRecord.Vehicle?.TransmissionType,
                BodyType = serviceRecord.Vehicle?.BodyType,
                EngineCapacityCc = serviceRecord.Vehicle?.EngineCapacityCc,
                EnginePowerHp = serviceRecord.Vehicle?.EnginePowerHp,
                EngineCode = serviceRecord.Vehicle?.EngineCode,
                ChassisNumber = serviceRecord.Vehicle?.ChassisNumber,

                Note = serviceRecord.ServiceReceptionNote,

                BankAccounts = bankAccounts,
                VehicleQrPublicUrl = vehicleQrPublicUrl,
                VehicleQrPngBytes = !string.IsNullOrWhiteSpace(vehicleQrPublicUrl)
                    ? CreateQrPng(vehicleQrPublicUrl)
                    : null,

                // ÖNEMLİ:
                // Servis panelinden oluşturulan belge tam SKF'dir.
                // Müşteri adı, plaka ve tüm bilgiler açık gelir.
                IsPublicMasked = false,

                RequestGroups = serviceRecord.RequestItems
                    .Where(x => !x.IsDeleted)
                    .OrderBy(x => x.Id)
                    .Select(item => new ServicePdfRequestGroupDto
                    {
                        Id = item.Id,
                        Title = item.Title,
                        Note = item.Note,
                        EstimatedAmount = item.EstimatedAmount,

                        Operations = serviceRecord.Operations
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

            var fileBytes = _pdfService.CreateServicePdf(request);

            var plate = serviceRecord.Vehicle?.Plate
                ?? serviceRecord.VehiclePlateSnapshot
                ?? "servis";

            var fileName = $"{plate}-{serviceRecord.RecordNumber}.pdf";

            return File(fileBytes, "application/pdf", fileName);
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

        private static byte[] CreateQrPng(string value)
        {
            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(value, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new PngByteQRCode(data);
            return qrCode.GetGraphic(10);
        }

        private string BuildPublicQrUrl(string code, string? publicBaseUrl)
        {
            if (!string.IsNullOrWhiteSpace(publicBaseUrl) &&
                Uri.TryCreate(publicBaseUrl.Trim(), UriKind.Absolute, out var baseUri) &&
                (baseUri.Scheme == Uri.UriSchemeHttps || baseUri.Scheme == Uri.UriSchemeHttp))
            {
                return new Uri(baseUri, $"/qr/{Uri.EscapeDataString(code)}").ToString();
            }

            return $"{Request.Scheme}://{Request.Host}/qr/{Uri.EscapeDataString(code)}";
        }
    }
}
