namespace AutoStock.Repositories.Entities
{
    public class WebPushSubscription
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public AppUser User { get; set; } = null!;

        public int? WorkshopId { get; set; }
        public Workshop? Workshop { get; set; }

        public string Endpoint { get; set; } = null!;
        public string P256dh { get; set; } = null!;
        public string Auth { get; set; } = null!;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? LastSuccessAt { get; set; }
        public DateTime? LastFailureAt { get; set; }

        public string? UserAgent { get; set; }
    }
}
