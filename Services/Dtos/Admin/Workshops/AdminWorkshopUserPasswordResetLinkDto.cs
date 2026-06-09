namespace AutoStock.Services.Dtos.Admin.Workshops
{
    public class AdminWorkshopUserPasswordResetLinkDto
    {
        public int UserId { get; set; }

        public int WorkshopId { get; set; }

        public string FullName { get; set; } = null!;

        public string UserName { get; set; } = null!;

        public string? Email { get; set; }

        public string? PhoneNumber { get; set; }

        public string PasswordResetToken { get; set; } = null!;

        public string? PasswordResetCode { get; set; }

        public DateTime PasswordResetExpiresAt { get; set; }
    }
}