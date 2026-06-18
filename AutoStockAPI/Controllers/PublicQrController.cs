using AutoStock.Repositories;
using AutoStock.Repositories.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace AutoStockAPI.Controllers
{
    [ApiController]
    [Route("qr")]
    [EnableRateLimiting("public-qr")]
    public class PublicQrController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PublicQrController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("{code}")]
        public async Task<IActionResult> Get(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return BadRequest();

            var qr = await _context.VehicleQrCodes
                .AsNoTracking()
                .Include(x => x.Workshop)
                .Include(x => x.Vehicle!)
                    .ThenInclude(x => x.VehicleBrand)
                .Include(x => x.Vehicle!)
                    .ThenInclude(x => x.VehicleModel)
                .FirstOrDefaultAsync(x =>
                    x.Code == code &&
                    x.Status == VehicleQrCodeStatus.Assigned &&
                    x.WorkshopId.HasValue &&
                    x.VehicleId.HasValue &&
                    x.Workshop != null &&
                    x.Workshop.IsActive &&
                    x.Vehicle != null &&
                    x.Vehicle.IsActive);

            if (qr == null || qr.Vehicle is null || qr.Workshop is null)
                return NotFound("QR kod bulunamadı.");

            var vehicle = qr.Vehicle;
            var workshopId = qr.WorkshopId!.Value;
            var vehicleId = qr.VehicleId!.Value;

            var workshopProfile = await _context.WorkshopProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.WorkshopId == workshopId);

            var records = await _context.ServiceRecords
                .AsNoTracking()
                .Where(x =>
                    x.WorkshopId == workshopId &&
                    x.VehicleId == vehicleId &&
                    x.Status == ServiceRecordStatus.Completed)
                .OrderByDescending(x => x.CompletedAt ?? x.CreatedAt)
                .Select(x => new
                {
                    x.Id,
                    x.RecordNumber,
                    ServiceDate = x.CompletedAt ?? x.CreatedAt,
                    Items = x.RequestItems
                        .Where(item => !item.IsDeleted)
                        .OrderBy(item => item.Id)
                        .Select(item => item.Title)
                        .Take(4)
                        .ToList()
                })
                .ToListAsync();

            return Ok(new
            {
                qr.Code,
                WorkshopName = !string.IsNullOrWhiteSpace(workshopProfile?.DisplayName)
                    ? workshopProfile.DisplayName
                    : qr.Workshop.Name,
                Vehicle = new
                {
                    Plate = MaskPlate(vehicle.Plate),
                    Brand = vehicle.VehicleBrand?.Name ?? "-",
                    Model = vehicle.VehicleModel?.Name ?? "-",
                    vehicle.ModelYear
                },
                Records = records
            });
        }

        private static string MaskPlate(string? plate)
        {
            if (string.IsNullOrWhiteSpace(plate))
                return "Plaka bilgisi";

            var clean = plate.Trim().ToUpperInvariant().Replace(" ", "");

            if (clean.Contains('*'))
                return clean;

            if (clean.Length <= 4)
                return clean[0] + "***";

            return $"{clean[..Math.Min(4, clean.Length)]}***";
        }
    }
}
