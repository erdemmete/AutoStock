using AutoStock.Repositories.Enums;

namespace AutoStock.Services.Dtos.SecurityTokens
{
    public class UserSecurityTokenValidationDto
    {
        public int TokenId { get; set; }

        public int UserId { get; set; }

        public string FullName { get; set; } = null!;

        public string UserName { get; set; } = null!;

        public string? Email { get; set; }

        public string? PhoneNumber { get; set; }

        public UserSecurityTokenPurpose Purpose { get; set; }

        public DateTime ExpiresAt { get; set; }
    }
}