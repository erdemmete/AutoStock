namespace AutoStock.Repositories.Entities
{
    public class Vehicle
    {
        public int Id { get; set; }

        public int WorkshopId { get; set; }

        public int CustomerId { get; set; }

        public Customer Customer { get; set; } = null!;

        public string Plate { get; set; } = null!;

        public int? VehicleBrandId { get; set; }

        public VehicleBrand? VehicleBrand { get; set; }

        public int? VehicleModelId { get; set; }

        public VehicleModel? VehicleModel { get; set; }

        public int? ModelYear { get; set; }

        public int? Mileage { get; set; }

        public string? VinNumber { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<ServiceRecord> ServiceRecords { get; set; } = new List<ServiceRecord>();
    }
}
