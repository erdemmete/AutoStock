namespace AutoStock.WEB.Models.ServiceRecords
{
    public class VehicleVariantViewModel
    {
        public int Id { get; set; }

        public int VehicleModelId { get; set; }

        public string Name { get; set; } = null!;

        public string? FuelType { get; set; }

        public string? TransmissionType { get; set; }

        public string? BodyType { get; set; }

        public int? EngineCapacityCc { get; set; }

        public int? EnginePowerHp { get; set; }

        public string? EngineCode { get; set; }

        public int? ModelYearFrom { get; set; }

        public int? ModelYearTo { get; set; }
    }
}