using AutoStock.Repositories.Enums;

namespace AutoStock.Repositories.Entities
{
    public class ServiceRecord
    {
        public int Id { get; set; }

        public int WorkshopId { get; set; }

        public int CustomerId { get; set; }

        public Customer Customer { get; set; } = null!;

        public int VehicleId { get; set; }

        public Vehicle Vehicle { get; set; } = null!;

        public ServiceRecordStatus Status { get; set; } = ServiceRecordStatus.Open;

        public string CustomerNameSnapshot { get; set; } = null!;

        public string CustomerPhoneSnapshot { get; set; } = null!;

        public string VehiclePlateSnapshot { get; set; } = null!;

        public string? VehicleBrandNameSnapshot { get; set; }

        public string? VehicleModelNameSnapshot { get; set; }

        public int? MileageSnapshot { get; set; }

        public string? CustomerComplaint { get; set; }

        public string? ServiceReceptionNote { get; set; }

        public string? RepairNote { get; set; }

        public decimal TotalAmount { get; set; }

        public bool ShowPricesOnPdf { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? CompletedAt { get; set; }

        public ICollection<ServiceOperation> Operations { get; set; } = new List<ServiceOperation>();

        public ICollection<ServiceRecordImage> Images { get; set; } = new List<ServiceRecordImage>();
    }
}
