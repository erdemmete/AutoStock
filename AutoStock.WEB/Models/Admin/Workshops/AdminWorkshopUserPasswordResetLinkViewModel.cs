namespace AutoStock.WEB.Models.Admin.Workshops
{
    public class AdminWorkshopUserPasswordResetLinkViewModel
    {
        public int UserId { get; set; }

        public int WorkshopId { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string UserName { get; set; } = string.Empty;

        public string? Email { get; set; }

        public string? PhoneNumber { get; set; }

        public string PasswordResetToken { get; set; } = string.Empty;

        public string? PasswordResetCode { get; set; }

        public DateTime PasswordResetExpiresAt { get; set; }
    }
}