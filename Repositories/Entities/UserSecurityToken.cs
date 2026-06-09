using AutoStock.Repositories.Enums;

namespace AutoStock.Repositories.Entities
{
    public class UserSecurityToken
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public AppUser User { get; set; } = null!;

        public string TokenHash { get; set; } = null!;
        public string? CodeHash { get; set; }

        public UserSecurityTokenPurpose Purpose { get; set; }

        public UserSecurityTokenDeliveryChannel DeliveryChannel { get; set; }

        public DateTime ExpiresAt { get; set; }

        public DateTime? UsedAt { get; set; }

        public DateTime? RevokedAt { get; set; }

        public DateTime CreatedAt { get; set; }

        public int? CreatedByUserId { get; set; }

        public string? ConsumedIpAddress { get; set; }

        public string? ConsumedUserAgent { get; set; }
    }
}