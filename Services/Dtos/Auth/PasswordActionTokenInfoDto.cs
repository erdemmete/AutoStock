using AutoStock.Repositories.Enums;

namespace AutoStock.Services.Dtos.Auth
{
    public class PasswordActionTokenInfoDto
    {
        public int UserId { get; set; }

        public string FullName { get; set; } = null!;

        public string UserName { get; set; } = null!;

        public string? Email { get; set; }

        public string? PhoneNumber { get; set; }

        public int? WorkshopId { get; set; }

        public string? WorkshopName { get; set; }

        public string? Role { get; set; }

        public UserSecurityTokenPurpose Purpose { get; set; }

        public DateTime ExpiresAt { get; set; }
    }
}