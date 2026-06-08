using AutoStock.Repositories.Enums;

namespace AutoStock.Repositories.Entities
{
    public class SupportRequest
    {
        public int Id { get; set; }

        public int WorkshopId { get; set; }

        public Workshop Workshop { get; set; } = null!;

        public int CreatedByUserId { get; set; }

        public AppUser CreatedByUser { get; set; } = null!;

        public SupportRequestType RequestType { get; set; }

        public SupportRequestStatus Status { get; set; }

        public SupportRequestPriority Priority { get; set; }

        public string Subject { get; set; } = null!;

        public string Description { get; set; } = null!;

        public string? RequestedUserFullName { get; set; }

        public string? RequestedUserPhone { get; set; }

        public string? RequestedUserEmail { get; set; }

        public SupportRequestedUserRole? RequestedUserRole { get; set; }

        public string? AdminResponse { get; set; }

        public int? RespondedByUserId { get; set; }

        public AppUser? RespondedByUser { get; set; }

        public DateTime? RespondedAt { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public DateTime? ClosedAt { get; set; }
    }
}