using AutoStock.Repositories.Enums;

namespace AutoStock.Services.Dtos.SupportRequests
{
    public class SupportRequestDetailDto
    {
        public int Id { get; set; }

        public int WorkshopId { get; set; }

        public string? WorkshopName { get; set; }

        public int CreatedByUserId { get; set; }

        public string CreatedByUserName { get; set; } = null!;

        public SupportRequestType RequestType { get; set; }

        public string RequestTypeText { get; set; } = null!;

        public SupportRequestStatus Status { get; set; }

        public string StatusText { get; set; } = null!;

        public SupportRequestPriority Priority { get; set; }

        public string PriorityText { get; set; } = null!;

        public string Subject { get; set; } = null!;

        public string Description { get; set; } = null!;

        public string? RequestedUserFullName { get; set; }

        public string? RequestedUserPhone { get; set; }

        public string? RequestedUserEmail { get; set; }

        public SupportRequestedUserRole? RequestedUserRole { get; set; }

        public string? RequestedUserRoleText { get; set; }

        // Eski tek cevap alanları geriye uyumluluk için duruyor.
        public string? AdminResponse { get; set; }

        public int? RespondedByUserId { get; set; }

        public string? RespondedByUserName { get; set; }

        public DateTime? RespondedAt { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public DateTime? ClosedAt { get; set; }

        public List<SupportRequestMessageDto> Messages { get; set; } = new();
    }
}
