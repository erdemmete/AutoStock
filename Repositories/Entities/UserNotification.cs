namespace AutoStock.Repositories.Entities
{
    public class UserNotification
    {
        public int Id { get; set; }

        public int NotificationId { get; set; }
        public Notification Notification { get; set; } = null!;

        public int UserId { get; set; }
        public AppUser User { get; set; } = null!;

        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
