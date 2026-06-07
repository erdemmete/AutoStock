using AutoStock.Repositories.Enums;

namespace AutoStock.Repositories.Entities
{
    public class AuditLog
    {
        public int Id { get; set; }

        public int? WorkshopId { get; set; }

        public int? UserId { get; set; }

        public string? UserFullName { get; set; }

        public string? UserRole { get; set; }

        public AuditActionType ActionType { get; set; }

        public AuditEntityType EntityType { get; set; }

        public int? EntityId { get; set; }

        public string Description { get; set; } = null!;

        public string? OldValuesJson { get; set; }

        public string? NewValuesJson { get; set; }

        public string? IpAddress { get; set; }

        public string? UserAgent { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}