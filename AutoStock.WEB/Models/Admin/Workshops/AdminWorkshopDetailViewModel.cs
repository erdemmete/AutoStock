namespace AutoStock.WEB.Models.Admin.Workshops
{
    public class AdminWorkshopDetailViewModel
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        public int SubscriptionStatus { get; set; }

        public string SubscriptionStatusText { get; set; } = string.Empty;

        public DateTime SubscriptionStartDate { get; set; }

        public DateTime? SubscriptionEndDate { get; set; }

        public string? SubscriptionNote { get; set; }

        public DateTime CreatedAt { get; set; }

        public AdminWorkshopProfileViewModel? Profile { get; set; }

        public List<AdminWorkshopUserViewModel> Users { get; set; } = new();
        public List<AdminWorkshopPartnerViewModel> Partners { get; set; } = new();
    }
}