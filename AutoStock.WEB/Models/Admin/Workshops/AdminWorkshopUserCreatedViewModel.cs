namespace AutoStock.WEB.Models.Admin.Workshops
{
    public class AdminWorkshopUserCreatedViewModel
    {
        public int UserId { get; set; }

        public int WorkshopId { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string UserName { get; set; } = string.Empty;

        public string? Email { get; set; }

        public string? PhoneNumber { get; set; }

        public string Role { get; set; } = string.Empty;

        public string PasswordSetupToken { get; set; } = string.Empty;

        public string? PasswordSetupCode { get; set; }

        public DateTime PasswordSetupExpiresAt { get; set; }
    }
}