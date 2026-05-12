using AutoStock.Repositories;
using AutoStock.Repositories.Enums;
using AutoStock.Services.Dtos.Pdfs;
using AutoStock.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoStock.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ServicePdfsController : ControllerBase
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
            var workshopIdClaim = User.FindFirst("workshopId")?.Value;

            if (!int.TryParse(workshopIdClaim, out var workshopId))
                return Unauthorized("Workshop bilgisi bulunamadı.");

            var serviceRecord = await _context.ServiceRecords
     .Include(x => x.Customer)
     .Include(x => x.Vehicle)
         .ThenInclude(x => x.VehicleBrand)
     .Include(x => x.Vehicle)
         .ThenInclude(x => x.VehicleModel)
     .Include(x => x.RequestItems)
     .Include(x => x.Operations)
     .FirstOrDefaultAsync(x =>
         x.Id == serviceRecordId &&
         x.WorkshopId == workshopId);

            if (serviceRecord == null)
                return NotFound("Servis kaydı bulunamadı.");

            var workshopName = await _context.Workshops
                .Where(x => x.Id == workshopId)
                .Select(x => x.Name)
                .FirstOrDefaultAsync();

            var request = new CreateServicePdfRequest
            {
                WorkshopName = workshopName ?? "Sente360",
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

                Note = serviceRecord.ServiceReceptionNote,

                RequestGroups = serviceRecord.RequestItems
        .OrderBy(x => x.Id)
        .Select(item => new ServicePdfRequestGroupDto
        {
            Id = item.Id,
            Title = item.Title,
            Note = item.Note,
            Operations = serviceRecord.Operations
    .Where(op => op.ServiceRequestItemId == item.Id)
    .OrderBy(op => op.Id)
    .Select(op => new ServicePdfItemDto
    {
        TypeText = op.Type == OperationType.Part ? "Parça" : "İşçilik",
        Name = op.Description,
        Quantity = op.Quantity,
        UnitPrice = op.UnitPrice,
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
    }
}