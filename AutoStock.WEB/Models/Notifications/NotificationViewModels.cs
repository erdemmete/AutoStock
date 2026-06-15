namespace AutoStock.WEB.Models.Notifications
{
    public class NotificationSummaryViewModel
    {
        public int UnreadCount { get; set; }

        public List<NotificationListItemViewModel> RecentNotifications { get; set; } = new();
    }

    public class NotificationListItemViewModel
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public string? ActionUrl { get; set; }

        public bool IsRead { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}