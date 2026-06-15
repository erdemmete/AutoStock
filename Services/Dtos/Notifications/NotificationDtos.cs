using AutoStock.Repositories.Enums;

namespace AutoStock.Services.Dtos.Notifications
{
    public class NotificationListQueryDto
    {
        private const int MaxPageSize = 50;

        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public bool? IsRead { get; set; }

        public void Normalize()
        {
            if (PageNumber < 1) PageNumber = 1;
            if (PageSize < 1) PageSize = 20;
            if (PageSize > MaxPageSize) PageSize = MaxPageSize;
        }
    }

    public class CreateNotificationDto
    {
        public int? WorkshopId { get; set; }
        public NotificationType Type { get; set; } = NotificationType.General;
        public string Title { get; set; } = null!;
        public string Message { get; set; } = null!;
        public NotificationRelatedEntityType RelatedEntityType { get; set; } = NotificationRelatedEntityType.None;
        public int? RelatedEntityId { get; set; }
        public string? ActionUrl { get; set; }
        public int? CreatedByUserId { get; set; }
    }

    public class NotificationListItemDto
    {
        public int Id { get; set; }
        public int NotificationId { get; set; }
        public string Title { get; set; } = null!;
        public string Message { get; set; } = null!;
        public int Type { get; set; }
        public string TypeText { get; set; } = null!;
        public int RelatedEntityType { get; set; }
        public int? RelatedEntityId { get; set; }
        public string? ActionUrl { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class NotificationHeaderDto
    {
        public int UnreadCount { get; set; }
        public List<NotificationListItemDto> Items { get; set; } = new();
    }
}
