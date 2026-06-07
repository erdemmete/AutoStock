using AutoStock.Repositories;
using AutoStock.Services.Constants;
using AutoStock.Services.Dtos.VehicleQrCodes;
using AutoStock.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoStock.API.Controllers
{
    [Authorize(Roles = AppRoles.Owner + "," + AppRoles.Staff)]
    [Route("api/[controller]")]
    [ApiController]
    public class VehicleQrCodesController : BaseApiController
    {
        private readonly AppDbContext _context;
        private readonly IDateTimeProvider _dateTimeProvider;

        public VehicleQrCodesController(
            AppDbContext context,
            IDateTimeProvider dateTimeProvider)
        {
            _context = context;
            _dateTimeProvider = dateTimeProvider;
        }

        [HttpPost("assign")]
        public async Task<IActionResult> Assign(AssignVehicleQrCodeRequest request)
        {
            var workshopIdResult = GetCurrentWorkshopId();

            if (workshopIdResult.IsFailure)
                return UnauthorizedResult(workshopIdResult);

            var workshopId = workshopIdResult.Data;

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
            qrCode.AssignedAt = _dateTimeProvider.Now;

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