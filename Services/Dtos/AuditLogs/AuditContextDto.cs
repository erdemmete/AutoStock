namespace AutoStock.Services.Dtos.AuditLogs
{
    public class AuditContextDto
    {
        public int? WorkshopId { get; set; }

        public int? UserId { get; set; }

        public string? UserFullName { get; set; }

        public string? UserRole { get; set; }

        public string? IpAddress { get; set; }

        public string? UserAgent { get; set; }
    }
}