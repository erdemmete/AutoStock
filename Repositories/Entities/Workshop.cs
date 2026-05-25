using AutoStock.Repositories.Enums;

namespace AutoStock.Repositories.Entities
{
    public class Workshop
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // ilişkiler
        public ICollection<WorkshopUser> WorkshopUsers { get; set; } = new List<WorkshopUser>();
        public bool IsActive { get; set; } = true;

        public WorkshopSubscriptionStatus SubscriptionStatus { get; set; }
            = WorkshopSubscriptionStatus.Trial;

        public DateTime SubscriptionStartDate { get; set; } = DateTime.UtcNow;

        public DateTime? SubscriptionEndDate { get; set; }

        public string? SubscriptionNote { get; set; }
        public WorkshopProfile? Profile { get; set; }

        public ICollection<WorkshopPartner> Partners { get; set; } = new List<WorkshopPartner>();
    }
}