using AutoStock.Repositories.Enums;

namespace AutoStock.Services.Dtos.SupportRequests
{
    public class CreateIssueSupportRequestDto
    {
        public string Subject { get; set; } = null!;

        public string Description { get; set; } = null!;

        public SupportRequestPriority Priority { get; set; } = SupportRequestPriority.Normal;
    }
}