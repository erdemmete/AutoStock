using AutoStock.Repositories.Enums;

namespace AutoStock.Services.Dtos.Admin.Workshops
{
    public class CreateAdminWorkshopRequestDto
    {
        public string WorkshopName { get; set; } = null!;

        public bool IsActive { get; set; } = true;

        public WorkshopSubscriptionStatus SubscriptionStatus { get; set; } = WorkshopSubscriptionStatus.Trial;

        public int? TrialDays { get; set; }

        public DateTime? SubscriptionEndDate { get; set; }

        public string? SubscriptionNote { get; set; }

        public string FirstUserFullName { get; set; } = null!;

        public string FirstUserName { get; set; } = null!;

        public string? FirstUserEmail { get; set; }

        public string FirstUserPassword { get; set; } = null!;

        // Sadece Owner veya Staff kabul edeceğiz.
        public string FirstUserRole { get; set; } = "Owner";
    }
}