namespace AutoStock.Services.Dtos.Admin.Workshops
{
    public class AdminWorkshopUserCreatedDto
    {
        public int UserId { get; set; }

        public int WorkshopId { get; set; }

        public string FullName { get; set; } = null!;

        public string UserName { get; set; } = null!;

        public string? Email { get; set; }

        public string? PhoneNumber { get; set; }

        public string Role { get; set; } = null!;

        public string PasswordSetupToken { get; set; } = null!;
        public string? PasswordSetupCode { get; set; }
        public DateTime PasswordSetupExpiresAt { get; set; }
    }
}