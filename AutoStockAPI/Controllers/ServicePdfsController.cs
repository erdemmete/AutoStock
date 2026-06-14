using AutoStock.Repositories;
using AutoStock.Repositories.Enums;
using AutoStock.Services.Constants;
using AutoStock.Services.Dtos.Pdfs;
using AutoStock.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
        public async Task<IActionResult> Create(int serviceRecordId)
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

            if (serviceRecord == null)
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
                
                            var workshopAddress = string.Join(" / ", new[]
                            {
                    workshopProfile?.AddressLine,
                    workshopProfile?.District,
                    workshopProfile?.City
                }.Where(x => !string.IsNullOrWhiteSpace(x)));

            var request = new CreateServicePdfRequest
            {
                WorkshopName = workshopName ?? "Servis adı belirtilmedi",
                WorkshopAddress = string.IsNullOrWhiteSpace(workshopAddress) ? null : workshopAddress,
                WorkshopPhone = workshopProfile?.PhoneNumber,
                RecordNumber = serviceRecord.RecordNumber,

                StatusText = serviceRecord.Status switch
                {
                    ServiceRecordStatus.Open => "Açık Kayıt",
                    ServiceRecordStatus.InProgress => "İşlemde",
                    ServiceRecordStatus.Completed => "Tamamlandı",
                    ServiceRecordStatus.Cancelled => "İptal Edildi",
                    _ => "Bilinmiyor"
                },

                CustomerName = serviceRecord.Customer?.FullName,
                CustomerPhone = serviceRecord.Customer?.PhoneNumber,
                CustomerEmail = serviceRecord.Customer?.Email,

                Plate = serviceRecord.Vehicle?.Plate,
                Brand = serviceRecord.Vehicle?.VehicleBrand?.Name,
                Model = serviceRecord.Vehicle?.VehicleModel?.Name,
                ModelYear = serviceRecord.Vehicle?.ModelYear?.ToString(),
                FuelLevelText = ToFuelLevelText(serviceRecord.FuelLevelSnapshot),
                VehicleVariantName = serviceRecord.Vehicle?.VehicleVariant?.Name,
                FuelType = serviceRecord.Vehicle?.FuelType,
                TransmissionType = serviceRecord.Vehicle?.TransmissionType,
                BodyType = serviceRecord.Vehicle?.BodyType,
                EngineCapacityCc = serviceRecord.Vehicle?.EngineCapacityCc,
                EnginePowerHp = serviceRecord.Vehicle?.EnginePowerHp,
                EngineCode = serviceRecord.Vehicle?.EngineCode,

                Note = serviceRecord.ServiceReceptionNote,
                RequestGroups = serviceRecord.RequestItems
                    .OrderBy(x => x.Id)
                    .Select(item => new ServicePdfRequestGroupDto
                    {
                        Id = item.Id,
                        Title = item.Title,
                        Note = item.Note,
                        EstimatedAmount = item.EstimatedAmount,
                        Operations = serviceRecord.Operations
                            .Where(op => op.ServiceRequestItemId == item.Id)
                            .OrderBy(op => op.Id)
                            .Select(op => new ServicePdfItemDto
                            {
                                TypeText = op.Type == OperationType.Part ? "Parça" : "İşçilik",
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

            var plate = serviceRecord.Vehicle?.Plate ?? "servis";
            var fileName = $"{plate}-{serviceRecord.RecordNumber}.pdf";

            return File(fileBytes, "application/pdf", fileName);
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