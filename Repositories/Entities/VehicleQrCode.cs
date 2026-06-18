using AutoStock.Repositories.Enums;

namespace AutoStock.Repositories.Entities
{
    public class VehicleQrCode
    {
        public int Id { get; set; }

        public string Code { get; set; } = null!;

        public VehicleQrCodeStatus Status { get; set; } = VehicleQrCodeStatus.Available;

        public int? WorkshopId { get; set; }

        public Workshop? Workshop { get; set; }

        public int? VehicleId { get; set; }

        public Vehicle? Vehicle { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? AssignedAt { get; set; }

        public DateTime? RetiredAt { get; set; }

        public int? CreatedByUserId { get; set; }

        public int? AssignedByUserId { get; set; }

        public int? RetiredByUserId { get; set; }
    }
}
