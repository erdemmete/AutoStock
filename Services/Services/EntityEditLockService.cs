using AutoStock.Repositories;
using AutoStock.Repositories.Entities;
using AutoStock.Repositories.Enums;
using AutoStock.Services.Constants;
using AutoStock.Services.Dtos.AuditLogs;
using AutoStock.Services.Dtos.Common;
using AutoStock.Services.Dtos.EditLocks;
using AutoStock.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
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

        private readonly AppDbContext _context;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IAuditLogService _auditLogService;

        public EntityEditLockService(
            AppDbContext context,
            IDateTimeProvider dateTimeProvider,
            IAuditLogService auditLogService)
        {
            _context = context;
            _dateTimeProvider = dateTimeProvider;
            _auditLogService = auditLogService;
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
                return ServiceResult<EntityEditLockDto>.Fail("Kayıt bulunamadı.", HttpStatusCode.NotFound);

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

                return ServiceResult<EntityEditLockDto>.Success(
                    isSameTab
                        ? ToDto(editLock, userId, now)
                        : ToDto(editLock, -1, now));
            }

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

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

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
                return ServiceResult<EntityEditLockDto>.Fail("Kayıt bulunamadı.", HttpStatusCode.NotFound);

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
                return ServiceResult<bool>.Fail(validation.ErrorMessages, HttpStatusCode.Conflict);

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
            }

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
                : ServiceResult<bool>.Fail(validation.ErrorMessages, HttpStatusCode.Conflict);
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
                return ServiceResult<EntityEditLock>.Fail(MissingLockMessage, HttpStatusCode.Conflict);

            var now = _dateTimeProvider.UtcNow;
            var editLock = await _context.EntityEditLocks
                .FirstOrDefaultAsync(x =>
                    x.WorkshopId == workshopId &&
                    x.EntityType == normalizedType &&
                    x.EntityId == entityId &&
                    x.LockedByUserId == userId &&
                    x.LockToken == lockToken,
                    cancellationToken);

            if (editLock is null || editLock.ExpiresAt <= now)
                return ServiceResult<EntityEditLock>.Fail(InvalidLockMessage, HttpStatusCode.Conflict);

            return ServiceResult<EntityEditLock>.Success(editLock);
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
