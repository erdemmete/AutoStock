using AutoStock.Repositories;
using AutoStock.Repositories.Enums;
using AutoStock.Services.Dtos.Common;
using AutoStock.Services.Dtos.Invoices;
using AutoStock.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace AutoStock.API.Controllers
{
    [ApiController]
    [Route("public/account-summary")]
    [EnableRateLimiting("public-qr")]
    public class PublicAccountSummariesController : BaseApiController
    {
        private readonly AppDbContext _context;
        private readonly IInvoiceService _invoiceService;

        public PublicAccountSummariesController(
            AppDbContext context,
            IInvoiceService invoiceService)
        {
            _context = context;
            _invoiceService = invoiceService;
        }

        [HttpGet("{qrCode}/{invoiceId:int}")]
        public async Task<IActionResult> Get(string qrCode, int invoiceId)
        {
            if (string.IsNullOrWhiteSpace(qrCode) || invoiceId <= 0)
            {
                return ToActionResult(
                    ServiceResult<InvoiceDetailDto>.Fail("Belge bağlantısı geçersiz."));
            }

            var access = await (
                    from qr in _context.VehicleQrCodes.AsNoTracking()
                    join invoice in _context.Invoices.AsNoTracking()
                        on qr.WorkshopId equals invoice.WorkshopId
                    join serviceRecord in _context.ServiceRecords.AsNoTracking()
                        on invoice.ServiceRecordId equals serviceRecord.Id
                    where qr.Code == qrCode &&
                          qr.Status == VehicleQrCodeStatus.Assigned &&
                          qr.WorkshopId.HasValue &&
                          qr.VehicleId.HasValue &&
                          qr.Workshop != null &&
                          qr.Workshop.IsActive &&
                          qr.Vehicle != null &&
                          qr.Vehicle.IsActive &&
                          invoice.Id == invoiceId &&
                          invoice.Status != InvoiceStatus.Cancelled &&
                          serviceRecord.WorkshopId == qr.WorkshopId.Value &&
                          serviceRecord.VehicleId == qr.VehicleId.Value
                    select new
                    {
                        WorkshopId = serviceRecord.WorkshopId
                    })
                .FirstOrDefaultAsync();

            if (access is null)
            {
                return NotFound(
                    ServiceResult<InvoiceDetailDto>.Fail(
                        "Servis hesap özeti bulunamadı veya bağlantı artık geçerli değil."));
            }

            var result = await _invoiceService.GetDetailAsync(invoiceId, access.WorkshopId);

            return ToActionResult(result);
        }
    }
}
