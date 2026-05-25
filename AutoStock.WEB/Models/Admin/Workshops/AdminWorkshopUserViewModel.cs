namespace AutoStock.WEB.Models.Admin.Workshops
{
    public class AdminWorkshopUserViewModel
    {
        public int UserId { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string UserName { get; set; } = string.Empty;

        public string? Email { get; set; }

        public string Role { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}