using AutoStock.Repositories.Enums;

namespace AutoStock.WEB.Models.SupportRequests
{
    public class CreateIssueSupportRequestViewModel
    {
        public string Subject { get; set; } = null!;

        public string Description { get; set; } = null!;

        public SupportRequestPriority Priority { get; set; } = SupportRequestPriority.Normal;
    }
}