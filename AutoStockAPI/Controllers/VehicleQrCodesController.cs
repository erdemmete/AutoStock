using AutoStock.Repositories;
using AutoStock.Repositories.Entities;
using AutoStock.Repositories.Enums;
using AutoStock.Services.Constants;
using AutoStock.Services.Dtos.AuditLogs;
using AutoStock.Services.Dtos.Common;
using AutoStock.Services.Dtos.VehicleQrCodes;
using AutoStock.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using System.Data;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace AutoStock.API.Controllers
{
    [Authorize(Roles = AppRoles.Owner + "," + AppRoles.Staff)]
    [Route("api/[controller]")]
    [ApiController]
    public class VehicleQrCodesController : BaseApiController
    {
        private const int QrCodeRandomByteCount = 16;
        private const int MaxQrGenerationAttempts = 6;

        private readonly AppDbContext _context;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IAuditLogService _auditLogService;
        private readonly IEntityEditLockService _entityEditLockService;
        private readonly ILogger<VehicleQrCodesController> _logger;

        public VehicleQrCodesController(
            AppDbContext context,
            IDateTimeProvider dateTimeProvider,
            IAuditLogService auditLogService,
            IEntityEditLockService entityEditLockService,
            ILogger<VehicleQrCodesController> logger)
        {
            _context = context;
            _dateTimeProvider = dateTimeProvider;
            _auditLogService = auditLogService;
            _entityEditLockService = entityEditLockService;
            _logger = logger;
        }

        [HttpPost("vehicles/{vehicleId:int}/create")]
        public async Task<IActionResult> CreateForVehicle(int vehicleId)
        {
            var workshopIdResult = GetCurrentWorkshopId();
            if (workshopIdResult.IsFailure)
                return UnauthorizedResult(workshopIdResult);

            var userIdResult = GetCurrentUserId();
            if (userIdResult.IsFailure)
                return UnauthorizedResult(userIdResult);

            var serviceRecordLockResult = await ValidateOptionalServiceRecordLockAsync(
                workshopIdResult.Data,
                userIdResult.Data);

            if (serviceRecordLockResult is not null)
                return serviceRecordLockResult;

            var result = await CreateOrReplaceAssignedQrAsync(
                workshopIdResult.Data,
                userIdResult.Data,
                vehicleId);

            return ToActionResult(result);
        }

        [HttpPost("assign")]
        public async Task<IActionResult> Assign(AssignVehicleQrCodeRequest request)
        {
            var workshopIdResult = GetCurrentWorkshopId();
            if (workshopIdResult.IsFailure)
                return UnauthorizedResult(workshopIdResult);

            var userIdResult = GetCurrentUserId();
            if (userIdResult.IsFailure)
                return UnauthorizedResult(userIdResult);

            var serviceRecordLockResult = await ValidateOptionalServiceRecordLockAsync(
                workshopIdResult.Data,
                userIdResult.Data);

            if (serviceRecordLockResult is not null)
                return serviceRecordLockResult;

            var normalizedCode = NormalizeQrCode(request.Code);

            if (string.IsNullOrWhiteSpace(normalizedCode))
                return BadRequest("QR kod zorunludur.");

            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            try
            {
                var vehicle = await GetActiveWorkshopVehicleAsync(request.VehicleId, workshopIdResult.Data);
                if (vehicle == null)
                    return BadRequest("Araç bulunamadı veya aktif değil.");

                var qrCode = await _context.VehicleQrCodes
                    .FirstOrDefaultAsync(x => x.Code == normalizedCode);

                if (qrCode == null)
                    return BadRequest("QR kod bulunamadı.");

                if (qrCode.Status != VehicleQrCodeStatus.Available ||
                    qrCode.VehicleId.HasValue ||
                    (qrCode.WorkshopId.HasValue && qrCode.WorkshopId.Value != workshopIdResult.Data))
                {
                    return BadRequest("Bu QR kod kullanılamaz.");
                }

                var now = _dateTimeProvider.Now;
                var oldAssignments = await RetireActiveVehicleQrCodesAsync(
                    workshopIdResult.Data,
                    vehicle.Id,
                    userIdResult.Data,
                    now);

                qrCode.WorkshopId = workshopIdResult.Data;
                qrCode.VehicleId = vehicle.Id;
                qrCode.Status = VehicleQrCodeStatus.Assigned;
                qrCode.AssignedAt = now;
                qrCode.AssignedByUserId = userIdResult.Data;

                await AddQrAuditAsync(
                    AuditActionType.Update,
                    qrCode.Id,
                    workshopIdResult.Data,
                    "QR kod araca bağlandı.",
                    new { qrCode.Code, vehicle.Id, retiredQrCount = oldAssignments.Count });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new VehicleQrCodeActionResultDto
                {
                    Id = qrCode.Id,
                    Code = qrCode.Code,
                    VehicleId = vehicle.Id,
                    ReplacedExistingQr = oldAssignments.Count > 0
                });
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogWarning(ex, "QR assignment concurrency or unique constraint failed.");
                return BadRequest("QR kod bağlanamadı. Lütfen sayfayı yenileyip tekrar deneyin.");
            }
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
                    x.Status == VehicleQrCodeStatus.Assigned &&
                    x.VehicleId.HasValue);

            if (qrCode == null)
            {
                return ToActionResult(ServiceResult<VehicleQrCodeResolveDto>.Fail(
                    "Bu QR kod bu servise atanmış aktif bir araca bağlı değil.",
                    HttpStatusCode.NotFound));
            }

            var vehicle = await _context.Vehicles
                .AsNoTracking()
                .Include(x => x.Customer)
                .Include(x => x.VehicleBrand)
                .Include(x => x.VehicleModel)
                .FirstOrDefaultAsync(x =>
                    x.Id == qrCode.VehicleId!.Value &&
                    x.WorkshopId == workshopId &&
                    x.IsActive);

            if (vehicle == null)
            {
                return ToActionResult(ServiceResult<VehicleQrCodeResolveDto>.Fail(
                    "QR koda bağlı aktif araç bulunamadı.",
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

        [HttpGet("vehicles/{vehicleId:int}/png")]
        public async Task<IActionResult> DownloadPng(int vehicleId, [FromQuery] string? publicBaseUrl)
        {
            var workshopIdResult = GetCurrentWorkshopId();
            if (workshopIdResult.IsFailure)
                return UnauthorizedResult(workshopIdResult);

            var qrCode = await _context.VehicleQrCodes
                .AsNoTracking()
                .Include(x => x.Vehicle)
                .FirstOrDefaultAsync(x =>
                    x.WorkshopId == workshopIdResult.Data &&
                    x.VehicleId == vehicleId &&
                    x.Status == VehicleQrCodeStatus.Assigned &&
                    x.Vehicle != null &&
                    x.Vehicle.IsActive);

            if (qrCode is null)
                return NotFound("Aktif QR kod bulunamadı.");

            var publicUrl = BuildPublicQrUrl(qrCode.Code, publicBaseUrl);
            var pngBytes = CreateQrPng(publicUrl);
            var fileName = $"{qrCode.Vehicle?.Plate ?? "arac"}-qr.png";

            return File(pngBytes, "image/png", fileName);
        }

        private async Task<ServiceResult<VehicleQrCodeActionResultDto>> CreateOrReplaceAssignedQrAsync(
            int workshopId,
            int userId,
            int vehicleId)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            try
            {
                var workshop = await _context.Workshops
                    .FirstOrDefaultAsync(x => x.Id == workshopId && x.IsActive);

                if (workshop is null)
                {
                    return ServiceResult<VehicleQrCodeActionResultDto>.Fail(
                        "Servis hesabı bulunamadı veya aktif değil.",
                        HttpStatusCode.BadRequest);
                }

                if (!workshop.QrGenerationEnabled)
                {
                    return ServiceResult<VehicleQrCodeActionResultDto>.Fail(
                        "Bu servis hesabı için QR oluşturma kapalı.",
                        HttpStatusCode.BadRequest);
                }

                var generatedQrCount = await _context.VehicleQrCodes
                    .CountAsync(x => x.WorkshopId == workshopId);

                if (generatedQrCount >= workshop.QrGenerationLimit)
                {
                    return ServiceResult<VehicleQrCodeActionResultDto>.Fail(
                        $"QR oluşturma limitiniz doldu. Limit: {workshop.QrGenerationLimit}.",
                        HttpStatusCode.BadRequest);
                }

                var vehicle = await GetActiveWorkshopVehicleAsync(vehicleId, workshopId);
                if (vehicle is null)
                {
                    return ServiceResult<VehicleQrCodeActionResultDto>.Fail(
                        "Araç bulunamadı veya aktif değil.",
                        HttpStatusCode.NotFound);
                }

                var now = _dateTimeProvider.Now;
                var retiredQrCodes = await RetireActiveVehicleQrCodesAsync(workshopId, vehicle.Id, userId, now);
                var code = await GenerateUniqueQrCodeAsync();

                var qrCode = new VehicleQrCode
                {
                    Code = code,
                    Status = VehicleQrCodeStatus.Assigned,
                    WorkshopId = workshopId,
                    VehicleId = vehicle.Id,
                    CreatedAt = now,
                    AssignedAt = now,
                    CreatedByUserId = userId,
                    AssignedByUserId = userId
                };

                _context.VehicleQrCodes.Add(qrCode);

                await _context.SaveChangesAsync();

                await AddQrAuditAsync(
                    AuditActionType.Create,
                    qrCode.Id,
                    workshopId,
                    "Araç QR kodu oluşturuldu.",
                    new { code, vehicle.Id });

                if (retiredQrCodes.Count > 0)
                {
                    await AddQrAuditAsync(
                        AuditActionType.Retire,
                        null,
                        workshopId,
                        "Eski araç QR kodu kullanım dışı bırakıldı.",
                        new { vehicle.Id, retiredCodes = retiredQrCodes.Select(x => x.Code).ToList() });

                    await AddQrAuditAsync(
                        AuditActionType.Update,
                        qrCode.Id,
                        workshopId,
                        "Araç QR kodu değiştirildi.",
                        new
                        {
                            vehicle.Id,
                            newCode = code,
                            retiredCodes = retiredQrCodes.Select(x => x.Code).ToList()
                        });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return ServiceResult<VehicleQrCodeActionResultDto>.Success(new VehicleQrCodeActionResultDto
                {
                    Id = qrCode.Id,
                    Code = qrCode.Code,
                    VehicleId = vehicle.Id,
                    ReplacedExistingQr = retiredQrCodes.Count > 0
                });
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogWarning(ex, "QR create/replace concurrency or unique constraint failed.");

                return ServiceResult<VehicleQrCodeActionResultDto>.Fail(
                    "QR oluşturulamadı. Lütfen sayfayı yenileyip tekrar deneyin.",
                    HttpStatusCode.Conflict);
            }
        }

        private async Task<Vehicle?> GetActiveWorkshopVehicleAsync(int vehicleId, int workshopId)
        {
            return await _context.Vehicles
                .FirstOrDefaultAsync(x =>
                    x.Id == vehicleId &&
                    x.WorkshopId == workshopId &&
                    x.IsActive);
        }

        private async Task<IActionResult?> ValidateOptionalServiceRecordLockAsync(int workshopId, int userId)
        {
            var serviceRecordId = GetServiceRecordIdHeader();
            if (!serviceRecordId.HasValue)
                return null;

            var result = await _entityEditLockService.ValidateAsync(
                "ServiceRecord",
                serviceRecordId.Value,
                GetEditLockToken(),
                workshopId,
                userId);

            return result.IsFailure ? ToActionResult(result) : null;
        }

        private async Task<List<VehicleQrCode>> RetireActiveVehicleQrCodesAsync(
            int workshopId,
            int vehicleId,
            int userId,
            DateTime now)
        {
            var activeQrCodes = await _context.VehicleQrCodes
                .Where(x =>
                    x.WorkshopId == workshopId &&
                    x.VehicleId == vehicleId &&
                    x.Status == VehicleQrCodeStatus.Assigned)
                .ToListAsync();

            foreach (var activeQrCode in activeQrCodes)
            {
                activeQrCode.Status = VehicleQrCodeStatus.Retired;
                activeQrCode.RetiredAt = now;
                activeQrCode.RetiredByUserId = userId;
            }

            return activeQrCodes;
        }

        private async Task<string> GenerateUniqueQrCodeAsync()
        {
            for (var attempt = 0; attempt < MaxQrGenerationAttempts; attempt++)
            {
                var code = GenerateSecureQrCode();

                var exists = await _context.VehicleQrCodes
                    .AnyAsync(x => x.Code == code);

                if (!exists)
                    return code;
            }

            throw new InvalidOperationException("Benzersiz QR kod üretilemedi.");
        }

        private static string GenerateSecureQrCode()
        {
            Span<byte> bytes = stackalloc byte[QrCodeRandomByteCount];
            RandomNumberGenerator.Fill(bytes);

            return $"S360-{ToBase32(bytes)}";
        }

        private static string ToBase32(ReadOnlySpan<byte> bytes)
        {
            const string alphabet = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ";
            var output = new StringBuilder();
            var buffer = 0;
            var bitsLeft = 0;

            foreach (var value in bytes)
            {
                buffer = (buffer << 8) | value;
                bitsLeft += 8;

                while (bitsLeft >= 5)
                {
                    output.Append(alphabet[(buffer >> (bitsLeft - 5)) & 31]);
                    bitsLeft -= 5;
                }
            }

            if (bitsLeft > 0)
            {
                output.Append(alphabet[(buffer << (5 - bitsLeft)) & 31]);
            }

            return output.ToString();
        }

        private static byte[] CreateQrPng(string value)
        {
            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(value, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new PngByteQRCode(data);
            return qrCode.GetGraphic(12);
        }

        private static string BuildPublicQrUrl(string code, string? publicBaseUrl)
        {
            if (!string.IsNullOrWhiteSpace(publicBaseUrl) &&
                Uri.TryCreate(publicBaseUrl.Trim(), UriKind.Absolute, out var baseUri) &&
                (baseUri.Scheme == Uri.UriSchemeHttps || baseUri.Scheme == Uri.UriSchemeHttp))
            {
                return new Uri(baseUri, $"/qr/{Uri.EscapeDataString(code)}").ToString();
            }

            return $"/qr/{Uri.EscapeDataString(code)}";
        }

        private async Task AddQrAuditAsync(
            AuditActionType actionType,
            int? entityId,
            int workshopId,
            string description,
            object newValues)
        {
            await _auditLogService.AddAsync(new AuditLogCreateDto
            {
                WorkshopId = workshopId,
                ActionType = actionType,
                EntityType = AuditEntityType.VehicleQrCode,
                EntityId = entityId,
                Description = description,
                NewValues = newValues
            });
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
