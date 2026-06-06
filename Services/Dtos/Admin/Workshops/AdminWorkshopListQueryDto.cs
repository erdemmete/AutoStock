using AutoStock.Repositories.Enums;

namespace AutoStock.Services.Dtos.Admin.Workshops
{
    public class AdminWorkshopListQueryDto
    {
        public string? Search { get; set; }

        public bool? IsActive { get; set; }

        public WorkshopSubscriptionStatus? SubscriptionStatus { get; set; }

        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 10;
    }
}