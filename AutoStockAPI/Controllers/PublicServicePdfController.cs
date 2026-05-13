using AutoStock.Repositories;
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

        public PublicServicePdfController(AppDbContext context, IPdfService pdfService)
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
                .FirstOrDefaultAsync(x => x.Code == qrCode);

            if (qr is null || !qr.VehicleId.HasValue)
                return NotFound("QR kod bulunamadı veya araca atanmamış.");

            var record = await _context.ServiceRecords
            .Include(x => x.Operations)
            .Include(x => x.RequestItems)
            
            .FirstOrDefaultAsync(x =>
                x.Id == serviceRecordId &&
        x.VehicleId == qr.VehicleId.Value);       

            if (record is null)
                return NotFound("Servis kaydı bulunamadı.");

            var workshop = await _context.Workshops
    .FirstOrDefaultAsync(x => x.Id == record.WorkshopId);

            var pdfRequest = new CreateServicePdfRequest
            {
                CustomerName = record.CustomerNameSnapshot,
                CustomerPhone = record.CustomerPhoneSnapshot,
                CustomerEmail = null,

                Plate = record.VehiclePlateSnapshot,
                Brand = record.VehicleBrandNameSnapshot,
                Model = record.VehicleModelNameSnapshot,
                ModelYear = null,

                Note = record.ServiceReceptionNote,
                WorkshopName = workshop?.Name ?? "Sente360",
                RecordNumber = record.RecordNumber,

                StatusText = record.Status switch
                {
                    ServiceRecordStatus.Open => "Açık Kayıt",
                    ServiceRecordStatus.InProgress => "İşlemde",
                    ServiceRecordStatus.Completed => "Tamamlandı",
                    ServiceRecordStatus.Cancelled => "İptal Edildi",
                    _ => "Bilinmiyor"
                },

                Operations = record.Operations
                    .OrderBy(x => x.Id)
                    .Select(x => new ServicePdfItemDto
                    {
                        Name = x.Description,
                        Quantity = x.Quantity,
                        UnitPrice = x.UnitPrice,
                        TotalPrice = x.TotalPrice,
                        TypeText = (int)x.Type == 1 ? "Parça" : "İşçilik",
                        Note = x.Note
                    })
                    .ToList(),

                RequestGroups = record.RequestItems
                    .OrderBy(x => x.Id)
                    .Select(item => new ServicePdfRequestGroupDto
                    {
                        Title = item.Title,
                        Note = item.Note,
                        Operations = record.Operations
                            .Where(op => op.ServiceRequestItemId == item.Id)
                            .OrderBy(op => op.Id)
                            .Select(op => new ServicePdfItemDto
                            {
                                Name = op.Description,
                                Quantity = op.Quantity,
                                UnitPrice = op.UnitPrice,
                                TotalPrice = op.TotalPrice,
                                TypeText = (int)op.Type == 1 ? "Parça" : "İşçilik",
                                Note = op.Note
                            })
                            .ToList()
                    })
                    .ToList()
            };

            var pdfBytes = _pdfService.CreateServicePdf(pdfRequest);

            var fileName = $"{record.RecordNumber}-servis-formu.pdf";

            return File(pdfBytes, "application/pdf", fileName);
        }
    }
}