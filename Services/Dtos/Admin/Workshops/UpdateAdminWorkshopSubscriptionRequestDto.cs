using AutoStock.Repositories.Enums;

namespace AutoStock.Services.Dtos.Admin.Workshops
{
    public class UpdateAdminWorkshopSubscriptionRequestDto
    {
        public bool IsActive { get; set; }

        public WorkshopSubscriptionStatus SubscriptionStatus { get; set; }

        public DateTime? SubscriptionEndDate { get; set; }

        public string? SubscriptionNote { get; set; }
    }
}