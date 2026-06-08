using AutoStock.Repositories.Enums;

namespace AutoStock.WEB.Models.SupportRequests
{
    public class CreateUserSupportRequestViewModel
    {
        public string RequestedUserFullName { get; set; } = null!;

        public string? RequestedUserPhone { get; set; }

        public string? RequestedUserEmail { get; set; }

        public SupportRequestedUserRole RequestedUserRole { get; set; } = SupportRequestedUserRole.Staff;

        public string? Note { get; set; }

        public SupportRequestPriority Priority { get; set; } = SupportRequestPriority.Normal;
    }
}