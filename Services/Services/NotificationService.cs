using AutoStock.Repositories;
using AutoStock.Repositories.Entities;
using AutoStock.Repositories.Enums;
using AutoStock.Services.Constants;
using AutoStock.Services.Dtos.Common;
using AutoStock.Services.Dtos.Notifications;
using AutoStock.Services.Dtos.WebPush;
using AutoStock.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AutoStock.Services.Services
{
    public class NotificationService : INotificationService
    {
        private readonly AppDbContext _context;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IWebPushSender _webPushSender;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            AppDbContext context,
            IDateTimeProvider dateTimeProvider,
            IWebPushSender webPushSender,
            ILogger<NotificationService> logger)
        {
            _context = context;
            _dateTimeProvider = dateTimeProvider;
            _webPushSender = webPushSender;
            _logger = logger;
        }

        public async Task<ServiceResult<bool>> CreateForUsersAsync(
            IEnumerable<int> userIds,
            CreateNotificationDto request)
        {
            var distinctUserIds = userIds
                .Where(x => x > 0)
                .Distinct()
                .ToList();

            if (!distinctUserIds.Any())
                return ServiceResult<bool>.Success(true);

            var activeUserIds = await _context.Users
                .AsNoTracking()
                .Where(x => distinctUserIds.Contains(x.Id) && x.IsActive)
                .Select(x => x.Id)
                .ToListAsync();

            if (!activeUserIds.Any())
                return ServiceResult<bool>.Success(true);

            var now = _dateTimeProvider.Now;

            var notification = new Notification
            {
                WorkshopId = request.WorkshopId,
                Type = request.Type,
                Title = NormalizeRequired(request.Title, "Bildirim", 180),
                Message = NormalizeRequired(request.Message, "Yeni bir bildiriminiz var.", 1000),
                RelatedEntityType = request.RelatedEntityType,
                RelatedEntityId = request.RelatedEntityId,
                ActionUrl = NormalizeNullable(request.ActionUrl, 500),
                CreatedByUserId = request.CreatedByUserId,
                CreatedAt = now
            };

            foreach (var userId in activeUserIds)
            {
                notification.UserNotifications.Add(new UserNotification
                {
                    UserId = userId,
                    IsRead = false,
                    CreatedAt = now
                });
            }

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            await TrySendWebPushAsync(notification, activeUserIds);

            return ServiceResult<bool>.Success(true);
        }

        public async Task<ServiceResult<bool>> CreateForAdminsAsync(CreateNotificationDto request)
        {
            var adminRoleId = await _context.Roles
                .AsNoTracking()
                .Where(x => x.Name == AppRoles.Admin)
                .Select(x => x.Id)
                .FirstOrDefaultAsync();

            if (adminRoleId <= 0)
                return ServiceResult<bool>.Success(true);

            var adminUserIds = await _context.UserRoles
                .AsNoTracking()
                .Where(x => x.RoleId == adminRoleId)
                .Select(x => x.UserId)
                .Distinct()
                .ToListAsync();

            return await CreateForUsersAsync(adminUserIds, request);
        }

        public async Task<ServiceResult<bool>> CreateForWorkshopOwnersAsync(
            int workshopId,
            CreateNotificationDto request)
        {
            var ownerUserIds = await _context.WorkshopUsers
                .AsNoTracking()
                .Where(x =>
                    x.WorkshopId == workshopId &&
                    x.Role == AppRoles.Owner &&
                    x.User.IsActive)
                .Select(x => x.UserId)
                .Distinct()
                .ToListAsync();

            request.WorkshopId ??= workshopId;

            return await CreateForUsersAsync(ownerUserIds, request);
        }

        public async Task<ServiceResult<bool>> CreateForWorkshopOwnersAndUsersAsync(
            int workshopId,
            IEnumerable<int> userIds,
            CreateNotificationDto request)
        {
            var ownerUserIds = await _context.WorkshopUsers
                .AsNoTracking()
                .Where(x =>
                    x.WorkshopId == workshopId &&
                    x.Role == AppRoles.Owner &&
                    x.User.IsActive)
                .Select(x => x.UserId)
                .Distinct()
                .ToListAsync();

            var allUserIds = ownerUserIds
                .Concat(userIds ?? Array.Empty<int>())
                .Where(x => x > 0)
                .Distinct()
                .ToList();

            request.WorkshopId ??= workshopId;

            return await CreateForUsersAsync(allUserIds, request);
        }

        public async Task<ServiceResult<NotificationHeaderDto>> GetHeaderAsync(int userId, int maxItems = 8)
        {
            if (maxItems < 1) maxItems = 8;
            if (maxItems > 20) maxItems = 20;

            var unreadCount = await _context.UserNotifications
                .AsNoTracking()
                .Where(x => x.UserId == userId && !x.IsRead)
                .CountAsync();

            var items = await _context.UserNotifications
                .AsNoTracking()
                .Include(x => x.Notification)
                .Where(x => x.UserId == userId)
                .OrderBy(x => x.IsRead)
                .ThenByDescending(x => x.Notification.CreatedAt)
                .ThenByDescending(x => x.Id)
                .Take(maxItems)
                .Select(x => MapToListItemDto(x))
                .ToListAsync();

            return ServiceResult<NotificationHeaderDto>.Success(new NotificationHeaderDto
            {
                UnreadCount = unreadCount,
                Items = items
            });
        }

        public async Task<ServiceResult<PagedResult<NotificationListItemDto>>> GetPagedAsync(
            NotificationListQueryDto query,
            int userId)
        {
            query ??= new NotificationListQueryDto();
            query.Normalize();

            var notificationsQuery = _context.UserNotifications
                .AsNoTracking()
                .Include(x => x.Notification)
                .Where(x => x.UserId == userId);

            if (query.IsRead.HasValue)
            {
                notificationsQuery = notificationsQuery.Where(x => x.IsRead == query.IsRead.Value);
            }

            var totalCount = await notificationsQuery.CountAsync();

            var items = await notificationsQuery
                .OrderBy(x => x.IsRead)
                .ThenByDescending(x => x.Notification.CreatedAt)
                .ThenByDescending(x => x.Id)
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(x => MapToListItemDto(x))
                .ToListAsync();

            return ServiceResult<PagedResult<NotificationListItemDto>>.Success(new PagedResult<NotificationListItemDto>
            {
                Items = items,
                PageNumber = query.PageNumber,
                PageSize = query.PageSize,
                TotalCount = totalCount
            });
        }

        public async Task<ServiceResult<bool>> MarkAsReadAsync(int userNotificationId, int userId)
        {
            var userNotification = await _context.UserNotifications
                .FirstOrDefaultAsync(x => x.Id == userNotificationId && x.UserId == userId);

            if (userNotification is null)
                return ServiceResult<bool>.Fail("Bildirim bulunamadı.");

            if (!userNotification.IsRead)
            {
                userNotification.IsRead = true;
                userNotification.ReadAt = _dateTimeProvider.Now;
                await _context.SaveChangesAsync();
            }

            return ServiceResult<bool>.Success(true);
        }

        public async Task<ServiceResult<bool>> MarkAllAsReadAsync(int userId)
        {
            var unreadNotifications = await _context.UserNotifications
                .Where(x => x.UserId == userId && !x.IsRead)
                .ToListAsync();

            if (!unreadNotifications.Any())
                return ServiceResult<bool>.Success(true);

            var now = _dateTimeProvider.Now;

            foreach (var item in unreadNotifications)
            {
                item.IsRead = true;
                item.ReadAt = now;
            }

            await _context.SaveChangesAsync();

            return ServiceResult<bool>.Success(true);
        }

        private static NotificationListItemDto MapToListItemDto(UserNotification userNotification)
        {
            var notification = userNotification.Notification;

            return new NotificationListItemDto
            {
                Id = userNotification.Id,
                NotificationId = notification.Id,
                Title = notification.Title,
                Message = notification.Message,
                Type = (int)notification.Type,
                TypeText = GetTypeText(notification.Type),
                RelatedEntityType = (int)notification.RelatedEntityType,
                RelatedEntityId = notification.RelatedEntityId,
                ActionUrl = notification.ActionUrl,
                IsRead = userNotification.IsRead,
                ReadAt = userNotification.ReadAt,
                CreatedAt = notification.CreatedAt
            };
        }

        private static string GetTypeText(NotificationType type)
        {
            return type switch
            {
                NotificationType.SupportRequestCreated => "Destek Talebi",
                NotificationType.SupportRequestAnswered => "Destek Yanıtı",
                NotificationType.SupportRequestStatusChanged => "Destek Durumu",
                NotificationType.InvoiceDocumentUploaded => "Fatura Yüklendi",
                NotificationType.InvoiceDocumentReuploaded => "Fatura Güncellendi",
                NotificationType.System => "Sistem",
                _ => "Bildirim"
            };
        }

        private static string NormalizeRequired(string? value, string fallback, int maxLength)
        {
            var normalized = string.IsNullOrWhiteSpace(value)
                ? fallback
                : value.Trim();

            return normalized.Length <= maxLength
                ? normalized
                : normalized[..maxLength];
        }

        private static string? NormalizeNullable(string? value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var normalized = value.Trim();

            return normalized.Length <= maxLength
                ? normalized
                : normalized[..maxLength];
        }

        private async Task TrySendWebPushAsync(Notification notification, IReadOnlyCollection<int> userIds)
        {
            var payload = BuildWebPushPayload(notification);

            if (payload is null)
                return;

            try
            {
                await _webPushSender.SendToUsersAsync(userIds, notification, payload);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Web Push delivery failed after in-app notification was saved. NotificationId: {NotificationId}",
                    notification.Id);
            }
        }

        private static WebPushPayloadDto? BuildWebPushPayload(Notification notification)
        {
            var url = string.IsNullOrWhiteSpace(notification.ActionUrl)
                ? "/Notifications"
                : notification.ActionUrl;

            return notification.Type switch
            {
                NotificationType.SupportRequestCreated => new WebPushPayloadDto
                {
                    Title = "Yeni Destek Talebi",
                    Body = "Yeni bir destek talebi oluşturuldu.",
                    Url = url,
                    NotificationId = notification.Id,
                    Tag = $"notification-{notification.Id}"
                },
                NotificationType.SupportRequestAnswered => new WebPushPayloadDto
                {
                    Title = "Sente360 Destek",
                    Body = "Destek talebinize yeni bir yanıt geldi.",
                    Url = url,
                    NotificationId = notification.Id,
                    Tag = $"notification-{notification.Id}"
                },
                NotificationType.InvoiceDocumentUploaded or NotificationType.InvoiceDocumentReuploaded => new WebPushPayloadDto
                {
                    Title = "Fatura Belgesi Hazır",
                    Body = "Muhasebeciniz talebiniz için fatura belgesini yükledi.",
                    Url = url,
                    NotificationId = notification.Id,
                    Tag = $"notification-{notification.Id}"
                },
                _ => null
            };
        }
    }
}
