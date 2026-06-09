using AutoStock.Repositories.Enums;

namespace AutoStock.Services.Dtos.SecurityTokens
{
    public class CreateUserSecurityTokenRequestDto
    {
        public int UserId { get; set; }

        public UserSecurityTokenPurpose Purpose { get; set; }

        public UserSecurityTokenDeliveryChannel DeliveryChannel { get; set; } =
            UserSecurityTokenDeliveryChannel.Manual;

        public TimeSpan? ValidFor { get; set; }

        public int? CreatedByUserId { get; set; }
    }
}