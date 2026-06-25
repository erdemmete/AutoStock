using AutoStock.Repositories;
using AutoStock.Repositories.Entities;
using AutoStock.Repositories.Enums;
using AutoStock.Services.Constants;
using AutoStock.Services.Dtos.AuditLogs;
using AutoStock.Services.Dtos.Common;
using AutoStock.Services.Dtos.EditLocks;
using AutoStock.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Net;
using System.Security.Cryptography;

namespace AutoStock.Services.Services
{
    public class EntityEditLockService : IEntityEditLockService
    {
        public const string InvoiceEntityType = "Invoice";
        public const string ServiceRecordEntityType = "ServiceRecord";
        public static readonly TimeSpan LockDuration = TimeSpan.FromMinutes(2);
        public static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(35);

        private const string MissingLockMessage = "Bu kaydı düzenlemek için aktif düzenleme kilidi gerekir. Sayfayı yenileyip tekrar deneyin.";
        private const string InvalidLockMessage = "Düzenleme kilidiniz süresi dolmuş veya geçersiz. Güncel verileri görmek için sayfayı yenileyin.";
        public const string MissingLockCode = "EDIT_LOCK_MISSING";
        public const string ExpiredLockCode = "EDIT_LOCK_EXPIRED";
        public const string HeldByAnotherUserCode = "EDIT_LOCK_HELD_BY_ANOTHER_USER";
        public const string InvalidLockCode = "EDIT_LOCK_INVALID";
        public const string EntityNotFoundCode = "EDIT_LOCK_ENTITY_NOT_FOUND";

        private readonly AppDbContext _context;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IAuditLogService _auditLogService;
        private readonly ILogger<EntityEditLockService> _logger;

        public EntityEditLockService(
            AppDbContext context,
            IDateTimeProvider dateTimeProvider,
            IAuditLogService auditLogService,
            ILogger<EntityEditLockService> logger)
        {
            _context = context;
            _dateTimeProvider = dateTimeProvider;
            _auditLogService = auditLogService;
            _logger = logger;
        }

        public async Task<ServiceResult<EntityEditLockDto>> AcquireAsync(
            string entityType,
            int entityId,
            string? existingLockToken,
            int workshopId,
            int userId,
            CancellationToken cancellationToken = default)
        {
            var normalizedType = NormalizeEntityType(entityType);
            if (normalizedType is null)
                return ServiceResult<EntityEditLockDto>.Fail("Kilit tipi geçersiz.", HttpStatusCode.BadRequest);

            if (!await EntityExistsAsync(normalizedType, entityId, workshopId, cancellationToken))
                return ServiceResult<EntityEditLockDto>.Fail(
                    "Kayıt bulunamadı.",
                    HttpStatusCode.NotFound,
                    EntityNotFoundCode);

            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            var now = _dateTimeProvider.UtcNow;

            var editLock = await _context.EntityEditLocks
                .Include(x => x.LockedByUser)
                .FirstOrDefaultAsync(x =>
                    x.WorkshopId == workshopId &&
                    x.EntityType == normalizedType &&
                    x.EntityId == entityId,
                    cancellationToken);

            if (editLock is not null && editLock.ExpiresAt > now)
            {
                await transaction.CommitAsync(cancellationToken);

                var isSameTab = editLock.LockedByUserId == userId &&
                    string.Equals(editLock.LockToken, existingLockToken, StringComparison.Ordinal);

                if (!isSameTab)
                {
                    _logger.LogWarning(
                        "Edit lock acquire denied. WorkshopId: {WorkshopId}, UserId: {UserId}, EntityType: {EntityType}, EntityId: {EntityId}, EventType: {EventType}",
                        workshopId, userId, normalizedType, entityId, HeldByAnotherUserCode);
                }

                if (!isSameTab)
                {
                    var heldResult = ServiceResult<EntityEditLockDto>.Fail(
                        $"Bu kayıt şu anda {GetDisplayName(editLock.LockedByUser)} tarafından düzenleniyor.",
                        HttpStatusCode.Conflict,
                        HeldByAnotherUserCode);
                    heldResult.Data = ToDto(editLock, -1, now);
                    return heldResult;
                }

                return ServiceResult<EntityEditLockDto>.Success(ToDto(editLock, userId, now));
            }

            var isReacquire = editLock is not null;
            var token = CreateLockToken();
            var expiresAt = now.Add(LockDuration);

            if (editLock is null)
            {
                editLock = new EntityEditLock
                {
                    WorkshopId = workshopId,
                    EntityType = normalizedType,
                    EntityId = entityId,
                    LockedByUserId = userId,
                    LockToken = token,
                    AcquiredAt = now,
                    LastHeartbeatAt = now,
                    ExpiresAt = expiresAt
                };

                _context.EntityEditLocks.Add(editLock);
            }
            else
            {
                editLock.LockedByUserId = userId;
                editLock.LockToken = token;
                editLock.AcquiredAt = now;
                editLock.LastHeartbeatAt = now;
                editLock.ExpiresAt = expiresAt;
            }

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _context.ChangeTracker.Clear();

                var concurrentLock = await _context.EntityEditLocks
                    .AsNoTracking()
                    .Include(x => x.LockedByUser)
                    .FirstOrDefaultAsync(x =>
                        x.WorkshopId == workshopId &&
                        x.EntityType == normalizedType &&
                        x.EntityId == entityId,
                        cancellationToken);

                _logger.LogWarning(
                    ex,
                    "Edit lock acquire concurrency conflict. WorkshopId: {WorkshopId}, UserId: {UserId}, EntityType: {EntityType}, EntityId: {EntityId}, EventType: {EventType}",
                    workshopId, userId, normalizedType, entityId, "ACQUIRE_CONFLICT");

                if (concurrentLock is not null && concurrentLock.ExpiresAt > now)
                {
                    var conflict = ServiceResult<EntityEditLockDto>.Fail(
                        $"Bu kayıt şu anda {GetDisplayName(concurrentLock.LockedByUser)} tarafından düzenleniyor.",
                        HttpStatusCode.Conflict,
                        HeldByAnotherUserCode);
                    conflict.Data = ToDto(concurrentLock, -1, now);
                    return conflict;
                }

                return ServiceResult<EntityEditLockDto>.Fail(
                    "Düzenleme oturumu başlatılamadı. Lütfen tekrar deneyin.",
                    HttpStatusCode.Conflict,
                    InvalidLockCode);
            }

            _logger.LogInformation(
                "Edit lock acquired. WorkshopId: {WorkshopId}, UserId: {UserId}, EntityType: {EntityType}, EntityId: {EntityId}, EventType: {EventType}",
                workshopId, userId, normalizedType, entityId, isReacquire ? "REACQUIRE" : "ACQUIRE");

            return ServiceResult<EntityEditLockDto>.Success(ToDto(editLock, userId, now));
        }

        public async Task<ServiceResult<EntityEditLockDto>> GetStatusAsync(
            string entityType,
            int entityId,
            int workshopId,
            int userId,
            CancellationToken cancellationToken = default)
        {
            var normalizedType = NormalizeEntityType(entityType);
            if (normalizedType is null)
                return ServiceResult<EntityEditLockDto>.Fail("Kilit tipi geçersiz.", HttpStatusCode.BadRequest);

            if (!await EntityExistsAsync(normalizedType, entityId, workshopId, cancellationToken))
                return ServiceResult<EntityEditLockDto>.Fail(
                    "Kayıt bulunamadı.",
                    HttpStatusCode.NotFound,
                    EntityNotFoundCode);

            var now = _dateTimeProvider.UtcNow;
            var editLock = await _context.EntityEditLocks
                .AsNoTracking()
                .Include(x => x.LockedByUser)
                .FirstOrDefaultAsync(x =>
                    x.WorkshopId == workshopId &&
                    x.EntityType == normalizedType &&
                    x.EntityId == entityId,
                    cancellationToken);

            return ServiceResult<EntityEditLockDto>.Success(ToDto(editLock, userId, now, normalizedType, entityId));
        }

        public async Task<ServiceResult<bool>> HeartbeatAsync(
            string entityType,
            int entityId,
            string? lockToken,
            int workshopId,
            int userId,
            CancellationToken cancellationToken = default)
        {
            var validation = await ValidateLockInternalAsync(entityType, entityId, lockToken, workshopId, userId, cancellationToken);
            if (validation.IsFailure || validation.Data is null)
            {
                _logger.LogWarning(
                    "Edit lock heartbeat failed. WorkshopId: {WorkshopId}, UserId: {UserId}, EntityType: {EntityType}, EntityId: {EntityId}, EventType: {EventType}",
                    workshopId, userId, entityType, entityId, validation.ErrorCode);
                return ServiceResult<bool>.Fail(
                    validation.ErrorMessages,
                    HttpStatusCode.Conflict,
                    validation.ErrorCode);
            }

            var now = _dateTimeProvider.UtcNow;
            validation.Data.LastHeartbeatAt = now;
            validation.Data.ExpiresAt = now.Add(LockDuration);

            await _context.SaveChangesAsync(cancellationToken);
            return ServiceResult<bool>.Success(true);
        }

        public async Task<ServiceResult<bool>> ReleaseAsync(
            string entityType,
            int entityId,
            string? lockToken,
            int workshopId,
            int userId,
            CancellationToken cancellationToken = default)
        {
            var normalizedType = NormalizeEntityType(entityType);
            if (normalizedType is null)
                return ServiceResult<bool>.Success(true);

            var editLock = await _context.EntityEditLocks
                .FirstOrDefaultAsync(x =>
                    x.WorkshopId == workshopId &&
                    x.EntityType == normalizedType &&
                    x.EntityId == entityId &&
                    x.LockedByUserId == userId &&
                    x.LockToken == lockToken,
                    cancellationToken);

            if (editLock is not null)
            {
                _context.EntityEditLocks.Remove(editLock);
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation(
                    "Edit lock released. WorkshopId: {WorkshopId}, UserId: {UserId}, EntityType: {EntityType}, EntityId: {EntityId}, EventType: {EventType}",
                    workshopId, userId, normalizedType, entityId, "RELEASE");
            }

            return ServiceResult<bool>.Success(true);
        }

        public async Task<ServiceResult<List<AdminEntityEditLockDto>>> GetWorkshopLocksForAdminAsync(
            int workshopId,
            CancellationToken cancellationToken = default)
        {
            if (!await _context.Workshops.AsNoTracking().AnyAsync(x => x.Id == workshopId, cancellationToken))
                return ServiceResult<List<AdminEntityEditLockDto>>.Fail("Servis bulunamadı.", HttpStatusCode.NotFound);

            var now = _dateTimeProvider.UtcNow;
            var locks = await _context.EntityEditLocks
                .AsNoTracking()
                .Where(x => x.WorkshopId == workshopId)
                .Select(x => new AdminEntityEditLockDto
                {
                    EntityType = x.EntityType,
                    EntityId = x.EntityId,
                    EntityReference = x.EntityType == ServiceRecordEntityType
                        ? _context.ServiceRecords
                            .Where(record => record.Id == x.EntityId && record.WorkshopId == workshopId)
                            .Select(record => record.RecordNumber)
                            .FirstOrDefault() ?? $"#{x.EntityId}"
                        : _context.Invoices
                            .Where(invoice => invoice.Id == x.EntityId && invoice.WorkshopId == workshopId)
                            .Select(invoice => invoice.InvoiceNumber)
                            .FirstOrDefault() ?? $"#{x.EntityId}",
                    LockedByDisplayName = !string.IsNullOrWhiteSpace(x.LockedByUser.FullName)
                        ? x.LockedByUser.FullName
                        : x.LockedByUser.UserName ?? "Kullanıcı",
                    AcquiredAt = x.AcquiredAt,
                    LastHeartbeatAt = x.LastHeartbeatAt,
                    ExpiresAt = x.ExpiresAt,
                    IsExpired = x.ExpiresAt <= now
                })
                .OrderBy(x => x.IsExpired)
                .ThenByDescending(x => x.LastHeartbeatAt)
                .ToListAsync(cancellationToken);

            return ServiceResult<List<AdminEntityEditLockDto>>.Success(locks);
        }

        public async Task<ServiceResult<bool>> ForceReleaseForAdminAsync(
            string entityType,
            int entityId,
            int workshopId,
            int adminUserId,
            CancellationToken cancellationToken = default)
        {
            var normalizedType = NormalizeEntityType(entityType);
            if (normalizedType is null)
                return ServiceResult<bool>.Fail("Kilit tipi geçersiz.", HttpStatusCode.BadRequest);

            if (!await _context.Workshops.AsNoTracking().AnyAsync(x => x.Id == workshopId, cancellationToken))
                return ServiceResult<bool>.Fail("Servis bulunamadı.", HttpStatusCode.NotFound);

            if (!await EntityExistsAsync(normalizedType, entityId, workshopId, cancellationToken))
                return ServiceResult<bool>.Fail(
                    "Kayıt bu servise ait değil veya bulunamadı.",
                    HttpStatusCode.NotFound,
                    EntityNotFoundCode);

            var editLock = await _context.EntityEditLocks.FirstOrDefaultAsync(x =>
                x.WorkshopId == workshopId &&
                x.EntityType == normalizedType &&
                x.EntityId == entityId,
                cancellationToken);

            if (editLock is null)
                return ServiceResult<bool>.Success(true);

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            _context.EntityEditLocks.Remove(editLock);
            await _auditLogService.AddAsync(new AuditLogCreateDto
            {
                WorkshopId = workshopId,
                UserId = adminUserId,
                UserRole = AppRoles.Admin,
                ActionType = AuditActionType.Remove,
                EntityType = normalizedType == InvoiceEntityType
                    ? AuditEntityType.Invoice
                    : AuditEntityType.ServiceRecord,
                EntityId = entityId,
                Description = "Admin düzenleme kilidini zorla kaldırdı.",
                OldValues = new
                {
                    editLock.LockedByUserId,
                    editLock.AcquiredAt,
                    editLock.LastHeartbeatAt,
                    editLock.ExpiresAt
                }
            }, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogWarning(
                "Edit lock force released by admin. WorkshopId: {WorkshopId}, UserId: {UserId}, EntityType: {EntityType}, EntityId: {EntityId}, EventType: {EventType}",
                workshopId, adminUserId, normalizedType, entityId, "ADMIN_FORCE_RELEASE");

            return ServiceResult<bool>.Success(true);
        }

        public async Task<ServiceResult<bool>> ForceReleaseAsync(
            string entityType,
            int entityId,
            int workshopId,
            int userId,
            string? userRole,
            CancellationToken cancellationToken = default)
        {
            if (!string.Equals(userRole, AppRoles.Owner, StringComparison.Ordinal) &&
                !string.Equals(userRole, AppRoles.Admin, StringComparison.Ordinal))
            {
                return ServiceResult<bool>.Fail("Bu işlem için yetkiniz bulunmuyor.", HttpStatusCode.Forbidden);
            }

            var normalizedType = NormalizeEntityType(entityType);
            if (normalizedType is null)
                return ServiceResult<bool>.Fail("Kilit tipi geçersiz.", HttpStatusCode.BadRequest);

            var editLock = await _context.EntityEditLocks
                .FirstOrDefaultAsync(x =>
                    x.WorkshopId == workshopId &&
                    x.EntityType == normalizedType &&
                    x.EntityId == entityId,
                    cancellationToken);

            if (editLock is null)
                return ServiceResult<bool>.Success(true);

            _context.EntityEditLocks.Remove(editLock);
            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.WriteAsync(new AuditLogCreateDto
            {
                WorkshopId = workshopId,
                UserId = userId,
                UserRole = userRole,
                ActionType = AuditActionType.Remove,
                EntityType = normalizedType == InvoiceEntityType
                    ? AuditEntityType.Invoice
                    : AuditEntityType.ServiceRecord,
                EntityId = entityId,
                Description = "Düzenleme kilidi zorla kaldırıldı.",
                OldValues = new
                {
                    editLock.LockedByUserId,
                    editLock.AcquiredAt,
                    editLock.ExpiresAt
                }
            }, cancellationToken);

            return ServiceResult<bool>.Success(true);
        }

        public async Task<ServiceResult<bool>> ValidateAsync(
            string entityType,
            int entityId,
            string? lockToken,
            int workshopId,
            int userId,
            CancellationToken cancellationToken = default)
        {
            var validation = await ValidateLockInternalAsync(entityType, entityId, lockToken, workshopId, userId, cancellationToken);

            return validation.IsSuccess
                ? ServiceResult<bool>.Success(true)
                : ServiceResult<bool>.Fail(
                    validation.ErrorMessages,
                    HttpStatusCode.Conflict,
                    validation.ErrorCode);
        }

        public async Task<ServiceResult<bool>> ValidateServiceRequestItemAsync(
            int requestItemId,
            string? lockToken,
            int workshopId,
            int userId,
            CancellationToken cancellationToken = default)
        {
            var serviceRecordId = await _context.ServiceRequestItems
                .AsNoTracking()
                .Where(x => x.Id == requestItemId && x.ServiceRecord.WorkshopId == workshopId)
                .Select(x => (int?)x.ServiceRecordId)
                .FirstOrDefaultAsync(cancellationToken);

            return serviceRecordId.HasValue
                ? await ValidateAsync(ServiceRecordEntityType, serviceRecordId.Value, lockToken, workshopId, userId, cancellationToken)
                : ServiceResult<bool>.Fail("Servis kaydı bulunamadı.", HttpStatusCode.NotFound);
        }

        public async Task<ServiceResult<bool>> ValidateServiceOperationAsync(
            int operationId,
            string? lockToken,
            int workshopId,
            int userId,
            CancellationToken cancellationToken = default)
        {
            var serviceRecordId = await _context.ServiceOperations
                .AsNoTracking()
                .Where(x => x.Id == operationId && x.ServiceRecord.WorkshopId == workshopId)
                .Select(x => (int?)x.ServiceRecordId)
                .FirstOrDefaultAsync(cancellationToken);

            return serviceRecordId.HasValue
                ? await ValidateAsync(ServiceRecordEntityType, serviceRecordId.Value, lockToken, workshopId, userId, cancellationToken)
                : ServiceResult<bool>.Fail("Servis kaydı bulunamadı.", HttpStatusCode.NotFound);
        }

        public async Task<ServiceResult<bool>> ValidateServiceRecordImageAsync(
            int imageId,
            string? lockToken,
            int workshopId,
            int userId,
            CancellationToken cancellationToken = default)
        {
            var serviceRecordId = await _context.ServiceRecordImages
                .AsNoTracking()
                .Where(x => x.Id == imageId && x.ServiceRecord.WorkshopId == workshopId)
                .Select(x => (int?)x.ServiceRecordId)
                .FirstOrDefaultAsync(cancellationToken);

            return serviceRecordId.HasValue
                ? await ValidateAsync(ServiceRecordEntityType, serviceRecordId.Value, lockToken, workshopId, userId, cancellationToken)
                : ServiceResult<bool>.Fail("Servis kaydı bulunamadı.", HttpStatusCode.NotFound);
        }

        private async Task<ServiceResult<EntityEditLock>> ValidateLockInternalAsync(
            string entityType,
            int entityId,
            string? lockToken,
            int workshopId,
            int userId,
            CancellationToken cancellationToken)
        {
            var normalizedType = NormalizeEntityType(entityType);
            if (normalizedType is null)
                return ServiceResult<EntityEditLock>.Fail("Kilit tipi geçersiz.", HttpStatusCode.BadRequest);

            if (string.IsNullOrWhiteSpace(lockToken))
            {
                LogValidationFailure(workshopId, userId, normalizedType, entityId, MissingLockCode);
                return ServiceResult<EntityEditLock>.Fail(
                    MissingLockMessage,
                    HttpStatusCode.Conflict,
                    MissingLockCode);
            }

            var now = _dateTimeProvider.UtcNow;
            var editLock = await _context.EntityEditLocks
                .FirstOrDefaultAsync(x =>
                    x.WorkshopId == workshopId &&
                    x.EntityType == normalizedType &&
                    x.EntityId == entityId &&
                    x.LockedByUserId == userId &&
                    x.LockToken == lockToken,
                    cancellationToken);

            if (editLock is null)
            {
                LogValidationFailure(workshopId, userId, normalizedType, entityId, InvalidLockCode);
                return ServiceResult<EntityEditLock>.Fail(
                    InvalidLockMessage,
                    HttpStatusCode.Conflict,
                    InvalidLockCode);
            }

            if (editLock.ExpiresAt <= now)
            {
                LogValidationFailure(workshopId, userId, normalizedType, entityId, ExpiredLockCode);
                return ServiceResult<EntityEditLock>.Fail(
                    "Düzenleme süreniz doldu. Kayıt başka bir kullanıcıda değilse yeniden düzenlemeye açılabilir.",
                    HttpStatusCode.Conflict,
                    ExpiredLockCode);
            }

            return ServiceResult<EntityEditLock>.Success(editLock);
        }

        private void LogValidationFailure(
            int workshopId,
            int userId,
            string entityType,
            int entityId,
            string eventType)
        {
            _logger.LogWarning(
                "Edit lock validation failed. WorkshopId: {WorkshopId}, UserId: {UserId}, EntityType: {EntityType}, EntityId: {EntityId}, EventType: {EventType}",
                workshopId, userId, entityType, entityId, eventType);
        }

        private async Task<bool> EntityExistsAsync(
            string entityType,
            int entityId,
            int workshopId,
            CancellationToken cancellationToken)
        {
            if (entityType == InvoiceEntityType)
            {
                return await _context.Invoices
                    .AnyAsync(x => x.Id == entityId && x.WorkshopId == workshopId, cancellationToken);
            }

            return await _context.ServiceRecords
                .AnyAsync(x => x.Id == entityId && x.WorkshopId == workshopId, cancellationToken);
        }

        private static EntityEditLockDto ToDto(
            EntityEditLock? editLock,
            int currentUserId,
            DateTime now,
            string? entityType = null,
            int? entityId = null)
        {
            if (editLock is null || editLock.ExpiresAt <= now)
            {
                return new EntityEditLockDto
                {
                    EntityType = entityType ?? string.Empty,
                    EntityId = entityId ?? 0,
                    IsEditable = false,
                    IsLockedByAnotherUser = false,
                    LockDurationSeconds = (int)LockDuration.TotalSeconds,
                    HeartbeatIntervalSeconds = (int)HeartbeatInterval.TotalSeconds
                };
            }

            var isCurrentUser = editLock.LockedByUserId == currentUserId;

            return new EntityEditLockDto
            {
                EntityType = editLock.EntityType,
                EntityId = editLock.EntityId,
                IsEditable = isCurrentUser,
                IsLockedByAnotherUser = !isCurrentUser,
                LockToken = isCurrentUser ? editLock.LockToken : null,
                LockedByDisplayName = !isCurrentUser
                    ? GetDisplayName(editLock.LockedByUser)
                    : null,
                AcquiredAt = editLock.AcquiredAt,
                ExpiresAt = editLock.ExpiresAt,
                LockDurationSeconds = (int)LockDuration.TotalSeconds,
                HeartbeatIntervalSeconds = (int)HeartbeatInterval.TotalSeconds
            };
        }

        private static string? NormalizeEntityType(string? entityType)
        {
            if (string.Equals(entityType, InvoiceEntityType, StringComparison.OrdinalIgnoreCase))
                return InvoiceEntityType;

            if (string.Equals(entityType, ServiceRecordEntityType, StringComparison.OrdinalIgnoreCase))
                return ServiceRecordEntityType;

            return null;
        }

        private static string GetDisplayName(AppUser? user)
        {
            if (user is null)
                return "başka bir kullanıcı";

            if (!string.IsNullOrWhiteSpace(user.FullName))
                return user.FullName;

            return user.UserName ?? "başka bir kullanıcı";
        }

        private static string CreateLockToken()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        }
    }
}
