namespace AutoStock.Services.Dtos.Vehicles
{
    public class VehicleSearchDto
    {
        public int Id { get; set; }

        public string Plate { get; set; } = null!;

        public int? CustomerId { get; set; }

        public int? CustomerType { get; set; }

        public string? CustomerName { get; set; }

        public string? CustomerPhone { get; set; }

        public string? CustomerEmail { get; set; }

        public string? CompanyName { get; set; }

        public string? AuthorizedPersonName { get; set; }

        public string? NationalIdentityNumber { get; set; }

        public string? TaxOffice { get; set; }

        public string? TaxNumber { get; set; }

        public string? AddressCity { get; set; }

        public string? AddressDistrict { get; set; }

        public string? CustomerAddress { get; set; }

        public int? BrandId { get; set; }

        public int? VehicleBrandId { get; set; }

        public string? BrandName { get; set; }

        public int? ModelId { get; set; }

        public int? VehicleModelId { get; set; }

        public string? ModelName { get; set; }

        public int? ModelYear { get; set; }

        public string? ChassisNumber { get; set; }

        public int? VehicleVariantId { get; set; }

        public string? VehicleVariantName { get; set; }

        public string? FuelType { get; set; }

        public string? TransmissionType { get; set; }

        public string? BodyType { get; set; }

        public int? EngineCapacityCc { get; set; }

        public int? EnginePowerHp { get; set; }

        public string? EngineCode { get; set; }
    }
}
