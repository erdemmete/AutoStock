namespace AutoStock.Repositories.Entities
{
    public class VehicleVariant
    {
        public int Id { get; set; }

        public int VehicleBrandId { get; set; }
        public VehicleBrand VehicleBrand { get; set; } = null!;

        public int VehicleModelId { get; set; }
        public VehicleModel VehicleModel { get; set; } = null!;

        public string Name { get; set; } = null!;

        public string? FuelType { get; set; }

        public string? TransmissionType { get; set; }

        public string? BodyType { get; set; }

        public int? EngineCapacityCc { get; set; }

        public int? EnginePowerHp { get; set; }

        public string? EngineCode { get; set; }

        public int? ModelYearFrom { get; set; }

        public int? ModelYearTo { get; set; }

        public bool IsActive { get; set; } = true;

        public int SortOrder { get; set; }
    }
}
