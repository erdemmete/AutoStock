using AutoStock.Repositories.Enums;

namespace AutoStock.Services.Dtos.SupportRequests
{
    public class SupportRequestListItemDto
    {
        public int Id { get; set; }

        public int WorkshopId { get; set; }

        public string? WorkshopName { get; set; }

        public SupportRequestType RequestType { get; set; }

        public string RequestTypeText { get; set; } = null!;

        public SupportRequestStatus Status { get; set; }

        public string StatusText { get; set; } = null!;

        public SupportRequestPriority Priority { get; set; }

        public string PriorityText { get; set; } = null!;

        public string Subject { get; set; } = null!;

        public string CreatedByUserName { get; set; } = null!;

        public DateTime CreatedAt { get; set; }

        public DateTime? RespondedAt { get; set; }

        public DateTime? ClosedAt { get; set; }
    }
}