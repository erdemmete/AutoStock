using System;
using System.Collections.Generic;
using System.Text;

namespace AutoStock.Repositories.Entities
{
    public class ServiceRecord
    {
        public int Id { get; set; }

        public int VehicleId { get; set; }
        public Vehicle Vehicle { get; set; } = null!;

        public int? EmployeeId { get; set; }
        public Employee? Employee { get; set; }

        public DateTime ServiceDate { get; set; } = DateTime.UtcNow;

        public string Complaint { get; set; } = null!; // Müşteri şikayeti
        public string? Diagnosis { get; set; } // Tespit
        public string? Notes { get; set; }

        public ServiceStatus Status { get; set; } = ServiceStatus.Pending;

        public decimal? LaborCost { get; set; }
        public decimal? TotalCost { get; set; }

        public ICollection<RepairRecord> RepairRecords { get; set; } = new List<RepairRecord>();
    }

    public enum ServiceStatus
    {
        Pending = 1,
        InProgress = 2,
        Completed = 3,
        Cancelled = 4
    }
}
