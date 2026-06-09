using AutoStock.Repositories.Enums;

namespace AutoStock.Services.Dtos.SecurityTokens
{
    public class UserSecurityTokenCreatedDto
    {
        public int UserId { get; set; }

        public string Token { get; set; } = null!;
        public string? Code { get; set; }

        public UserSecurityTokenPurpose Purpose { get; set; }

        public UserSecurityTokenDeliveryChannel DeliveryChannel { get; set; }

        public DateTime ExpiresAt { get; set; }
    }
}