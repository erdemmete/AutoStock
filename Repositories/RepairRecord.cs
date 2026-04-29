using System;
using System.Collections.Generic;
using System.Text;

namespace AutoStock.Repositories
{
    public class RepairRecord
    {
        public int Id { get; set; }

        public int ServiceRecordId { get; set; }
        public ServiceRecord ServiceRecord { get; set; } = null!;

        public string RepairDescription { get; set; } = null!; // Örn: Fren balatası değişti
        public string? UsedParts { get; set; } // Başlangıçta string olabilir

        public decimal? PartCost { get; set; }
        public decimal? LaborCost { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
