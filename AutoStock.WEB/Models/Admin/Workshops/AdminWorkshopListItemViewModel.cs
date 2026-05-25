namespace AutoStock.WEB.Models.Admin.Workshops
{
    public class AdminWorkshopListItemViewModel
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        public int SubscriptionStatus { get; set; }

        public string SubscriptionStatusText { get; set; } = string.Empty;

        public DateTime SubscriptionStartDate { get; set; }

        public DateTime? SubscriptionEndDate { get; set; }

        public int UserCount { get; set; }

        public DateTime CreatedAt { get; set; }

        public bool IsExpired { get; set; }
    }
}