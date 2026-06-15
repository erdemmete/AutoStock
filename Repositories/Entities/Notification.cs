using AutoStock.Repositories.Enums;

namespace AutoStock.Repositories.Entities
{
    public class Notification
    {
        public int Id { get; set; }

        public int? WorkshopId { get; set; }
        public Workshop? Workshop { get; set; }

        public NotificationType Type { get; set; } = NotificationType.General;

        public string Title { get; set; } = null!;
        public string Message { get; set; } = null!;

        public NotificationRelatedEntityType RelatedEntityType { get; set; } = NotificationRelatedEntityType.None;
        public int? RelatedEntityId { get; set; }

        public string? ActionUrl { get; set; }

        public int? CreatedByUserId { get; set; }
        public AppUser? CreatedByUser { get; set; }

        public DateTime CreatedAt { get; set; }

        public ICollection<UserNotification> UserNotifications { get; set; } = new List<UserNotification>();
    }
}
