using AutoStock.Repositories.Enums;

namespace AutoStock.Services.Dtos.ServiceRecords
{
    public class ServiceRecordImageDto
    {
        public int Id { get; set; }

        public int ServiceRecordId { get; set; }

        public ServiceImageType Type { get; set; }

        public string TypeText { get; set; } = null!;

        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}