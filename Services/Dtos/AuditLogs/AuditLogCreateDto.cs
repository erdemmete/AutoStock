using AutoStock.Repositories.Enums;

namespace AutoStock.Services.Dtos.AuditLogs
{
    public class AuditLogCreateDto
    {
        public int? WorkshopId { get; set; }

        public int? UserId { get; set; }

        public string? UserFullName { get; set; }

        public string? UserRole { get; set; }

        public AuditActionType ActionType { get; set; }

        public AuditEntityType EntityType { get; set; }

        public int? EntityId { get; set; }

        public string Description { get; set; } = null!;

        public object? OldValues { get; set; }

        public object? NewValues { get; set; }

        public string? IpAddress { get; set; }

        public string? UserAgent { get; set; }
    }
}