using AutoStock.Repositories.Enums;

namespace AutoStock.Services.Dtos.Admin.Workshops
{
    public class AdminWorkshopDetailDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!;

        public bool IsActive { get; set; }

        public WorkshopSubscriptionStatus SubscriptionStatus { get; set; }

        public string SubscriptionStatusText => SubscriptionStatus.ToString();

        public DateTime SubscriptionStartDate { get; set; }

        public DateTime? SubscriptionEndDate { get; set; }

        public string? SubscriptionNote { get; set; }

        public DateTime CreatedAt { get; set; }

        public List<AdminWorkshopUserDto> Users { get; set; } = new();
        public AdminWorkshopProfileDto? Profile { get; set; }
    }
}