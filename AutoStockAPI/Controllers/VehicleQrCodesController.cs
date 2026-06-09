using AutoStock.Repositories;
using AutoStock.Repositories.Enums;
using AutoStock.Services.Constants;
using AutoStock.Services.Dtos.Common;
using AutoStock.Services.Dtos.VehicleQrCodes;
using AutoStock.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;

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

            var normalizedCode = NormalizeQrCode(request.Code);

            if (string.IsNullOrWhiteSpace(normalizedCode))
                return BadRequest("QR kod zorunludur.");

            var vehicle = await _context.Vehicles
                .FirstOrDefaultAsync(x =>
                    x.Id == request.VehicleId &&
                    x.WorkshopId == workshopId);

            if (vehicle == null)
                return BadRequest("Araç bulunamadı.");

            var qrCode = await _context.VehicleQrCodes
                .FirstOrDefaultAsync(x => x.Code == normalizedCode);

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

        [HttpGet("resolve")]
        public async Task<IActionResult> Resolve([FromQuery] string code)
        {
            var workshopIdResult = GetCurrentWorkshopId();

            if (workshopIdResult.IsFailure)
                return UnauthorizedResult(workshopIdResult);

            var workshopId = workshopIdResult.Data;
            var normalizedCode = NormalizeQrCode(code);

            if (string.IsNullOrWhiteSpace(normalizedCode))
            {
                return ToActionResult(ServiceResult<VehicleQrCodeResolveDto>.Fail(
                    "QR kod okunamadı.",
                    HttpStatusCode.BadRequest));
            }

            var qrCode = await _context.VehicleQrCodes
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Code == normalizedCode &&
                    x.WorkshopId == workshopId &&
                    x.IsAssigned &&
                    x.VehicleId.HasValue);

            if (qrCode == null)
            {
                return ToActionResult(ServiceResult<VehicleQrCodeResolveDto>.Fail(
                    "Bu QR kod bu servise atanmış bir araca bağlı değil.",
                    HttpStatusCode.NotFound));
            }

            var vehicle = await _context.Vehicles
                .AsNoTracking()
                .Include(x => x.Customer)
                .Include(x => x.VehicleBrand)
                .Include(x => x.VehicleModel)
                .FirstOrDefaultAsync(x =>
                    x.Id == qrCode.VehicleId!.Value &&
                    x.WorkshopId == workshopId);

            if (vehicle == null)
            {
                return ToActionResult(ServiceResult<VehicleQrCodeResolveDto>.Fail(
                    "QR koda bağlı araç bulunamadı.",
                    HttpStatusCode.NotFound));
            }

            var openServiceRecord = await _context.ServiceRecords
                .AsNoTracking()
                .Where(x =>
                    x.WorkshopId == workshopId &&
                    x.VehicleId == vehicle.Id &&
                    (x.Status == ServiceRecordStatus.Open ||
                     x.Status == ServiceRecordStatus.InProgress))
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new
                {
                    x.Id,
                    x.RecordNumber
                })
                .FirstOrDefaultAsync();

            var customerName = "Müşteri";

            if (vehicle.Customer != null)
            {
                customerName = !string.IsNullOrWhiteSpace(vehicle.Customer.CompanyName)
                    ? vehicle.Customer.CompanyName
                    : vehicle.Customer.FullName ?? "Müşteri";
            }

            var response = new VehicleQrCodeResolveDto
            {
                Code = normalizedCode,
                VehicleId = vehicle.Id,
                CustomerId = vehicle.CustomerId,
                Plate = vehicle.Plate,
                CustomerName = customerName,
                VehicleBrandName = vehicle.VehicleBrand?.Name,
                VehicleModelName = vehicle.VehicleModel?.Name,
                OpenServiceRecordId = openServiceRecord?.Id,
                OpenServiceRecordNumber = openServiceRecord?.RecordNumber
            };

            return ToActionResult(ServiceResult<VehicleQrCodeResolveDto>.Success(response));
        }

        private static string NormalizeQrCode(string? code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return string.Empty;

            code = code.Trim();

            if (Uri.TryCreate(code, UriKind.Absolute, out var uri))
            {
                code = uri.Segments.LastOrDefault()?.Trim('/') ?? code;
            }

            return code.Trim();
        }
    }
}