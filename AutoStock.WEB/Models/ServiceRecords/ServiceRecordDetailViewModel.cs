namespace AutoStock.WEB.Models.ServiceRecords
{


public class ServiceRecordDetailViewModel
    {
        public int Id { get; set; }

        public string RecordNumber { get; set; } = null!;

        public int Status { get; set; }

        public string CustomerName { get; set; } = null!;

        public string CustomerPhone { get; set; } = null!;

        public string VehiclePlate { get; set; } = null!;

        public string? VehicleBrandName { get; set; }

        public string? VehicleModelName { get; set; }

        public string? ChassisNumber { get; set; }

        public int? Mileage { get; set; }

        public string? CustomerComplaint { get; set; }

        public string? ServiceReceptionNote { get; set; }

        public string? RepairNote { get; set; }

        public decimal TotalAmount { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        public int VehicleId { get; set; }

        public List<ServiceOperationViewModel> Operations { get; set; } = new();

        public List<ServiceRequestItemViewModel> RequestItems { get; set; } = new();
    }
}
