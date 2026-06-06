namespace AutoStock.WEB.Models.Admin.Workshops
{
    public class AdminWorkshopListQueryViewModel
    {
        public string? Search { get; set; }

        public bool? IsActive { get; set; }

        public int? SubscriptionStatus { get; set; }

        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 10;
    }
}