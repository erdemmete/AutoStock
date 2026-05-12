namespace AutoStock.Repositories.Entities
{
    public class VehicleQrCode
    {
        public int Id { get; set; }

        public string Code { get; set; } = null!;

        public int? WorkshopId { get; set; }

        public int? VehicleId { get; set; }

        public Vehicle? Vehicle { get; set; }

        public bool IsAssigned { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? AssignedAt { get; set; }
    }
}