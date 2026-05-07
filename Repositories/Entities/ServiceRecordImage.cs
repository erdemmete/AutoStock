using AutoStock.Repositories.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace AutoStock.Repositories.Entities
{
    public class ServiceRecordImage
    {
        public int Id { get; set; }

        public int ServiceRecordId { get; set; }

        public ServiceRecord ServiceRecord { get; set; } = null!;

        public ServiceImageType Type { get; set; }

        public string FilePath { get; set; } = null!;

        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
