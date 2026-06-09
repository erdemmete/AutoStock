namespace AutoStock.WEB.Models.Auth
{
    public class PasswordActionTokenInfoViewModel
    {
        public int UserId { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string UserName { get; set; } = string.Empty;

        public string? Email { get; set; }

        public string? PhoneNumber { get; set; }

        public int? WorkshopId { get; set; }

        public string? WorkshopName { get; set; }

        public string? Role { get; set; }

        public int Purpose { get; set; }

        public DateTime ExpiresAt { get; set; }
    }
}