namespace AutoStock.WEB.Models.Admin.Workshops
{
    public class AdminWorkshopPartnerViewModel
    {
        public int Id { get; set; }

        public int WorkshopId { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string? Title { get; set; }

        public string? PhoneNumber { get; set; }

        public string? Email { get; set; }

        public bool IsPrimary { get; set; }

        public string? Note { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}