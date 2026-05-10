using System;
using System.Collections.Generic;
using System.Text;

namespace AutoStock.Repositories.Entities
{
    public class ServiceRequestItem
    {
        public int Id { get; set; }

        public int ServiceRecordId { get; set; }

        public ServiceRecord ServiceRecord { get; set; } = null!;

        public string Title { get; set; } = null!;

        public string? Note { get; set; }

        public string? RepairDetail { get; set; }

        public bool IsResolved { get; set; }
        public decimal? EstimatedAmount { get; set; }

        public decimal? FinalAmount { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public ICollection<ServiceOperation> Operations { get; set; } = new List<ServiceOperation>();
    }
}
