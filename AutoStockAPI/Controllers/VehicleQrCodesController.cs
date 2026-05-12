using AutoStock.Repositories;

using AutoStock.Services.Dtos.VehicleQrCodes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoStockAPI.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class VehicleQrCodesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public VehicleQrCodesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("assign")]
        public async Task<IActionResult> Assign(AssignVehicleQrCodeRequest request)
        {
            var workshopIdClaim = User.FindFirst("workshopId")?.Value;

            if (!int.TryParse(workshopIdClaim, out var workshopId))
                return Unauthorized("Workshop bilgisi bulunamadı.");

            if (string.IsNullOrWhiteSpace(request.Code))
                return BadRequest("QR kod zorunludur.");

            var vehicle = await _context.Vehicles
                .FirstOrDefaultAsync(x =>
                    x.Id == request.VehicleId &&
                    x.WorkshopId == workshopId);

            if (vehicle == null)
                return BadRequest("Araç bulunamadı.");

            var qrCode = await _context.VehicleQrCodes
                .FirstOrDefaultAsync(x => x.Code == request.Code.Trim());

            if (qrCode == null)
                return BadRequest("QR kod bulunamadı.");

            if (qrCode.IsAssigned)
                return BadRequest("Bu QR kod daha önce atanmış.");

            qrCode.WorkshopId = workshopId;
            qrCode.VehicleId = vehicle.Id;
            qrCode.IsAssigned = true;
            qrCode.AssignedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                qrCode.Id,
                qrCode.Code,
                qrCode.VehicleId
            });
        }
    }
}