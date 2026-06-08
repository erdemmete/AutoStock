using AutoStock.Repositories.Enums;

namespace AutoStock.Services.Dtos.SupportRequests
{
    public class CreateUserSupportRequestDto
    {
        public string RequestedUserFullName { get; set; } = null!;

        public string? RequestedUserPhone { get; set; }

        public string? RequestedUserEmail { get; set; }

        public SupportRequestedUserRole RequestedUserRole { get; set; }

        public string? Note { get; set; }

        public SupportRequestPriority Priority { get; set; } = SupportRequestPriority.Normal;
    }
}