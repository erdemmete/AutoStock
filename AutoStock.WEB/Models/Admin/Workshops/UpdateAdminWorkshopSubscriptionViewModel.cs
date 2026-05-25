namespace AutoStock.WEB.Models.Admin.Workshops
{
    public class UpdateAdminWorkshopSubscriptionViewModel
    {
        public int WorkshopId { get; set; }

        public bool IsActive { get; set; }

        public int SubscriptionStatus { get; set; }

        public DateTime? SubscriptionEndDate { get; set; }

        public string? SubscriptionNote { get; set; }
    }
}