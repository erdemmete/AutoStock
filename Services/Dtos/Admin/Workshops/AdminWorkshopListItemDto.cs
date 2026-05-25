using AutoStock.Repositories.Enums;

namespace AutoStock.Services.Dtos.Admin.Workshops
{
    public class AdminWorkshopListItemDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!;

        public bool IsActive { get; set; }

        public WorkshopSubscriptionStatus SubscriptionStatus { get; set; }

        public string SubscriptionStatusText => SubscriptionStatus.ToString();

        public DateTime SubscriptionStartDate { get; set; }

        public DateTime? SubscriptionEndDate { get; set; }

        public int UserCount { get; set; }

        public DateTime CreatedAt { get; set; }

        public bool IsExpired =>
            SubscriptionEndDate.HasValue &&
            SubscriptionEndDate.Value < DateTime.UtcNow;
    }
}