using AutoStock.Repositories;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoStockAPI.Controllers
{
    [ApiController]
    [Route("qr")]
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
    .Include(x => x.Vehicle)
        .ThenInclude(x => x.VehicleBrand)
    .Include(x => x.Vehicle)
        .ThenInclude(x => x.VehicleModel)
    .FirstOrDefaultAsync(x => x.Code == code);

            if (qr == null)
                return NotFound("QR kod bulunamadı.");

            if (!qr.VehicleId.HasValue)
                return BadRequest("QR kod henüz bir araca atanmadı.");

            var records = await _context.ServiceRecords
                .Where(x => x.VehicleId == qr.VehicleId.Value)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new
                {
                    x.Id,
                    x.RecordNumber,
                    x.Status,
                    x.CreatedAt,
                    x.TotalAmount
                })
                .ToListAsync();

            return Ok(new
            {
                qr.Code,
                Vehicle = new
                {
                    qr.Vehicle!.Plate,
                    Brand = qr.Vehicle.VehicleBrand?.Name ?? "-",
                    Model = qr.Vehicle.VehicleModel?.Name ?? "-",
                    qr.Vehicle.ModelYear
                },
                Records = records
            });
        }
    }
}