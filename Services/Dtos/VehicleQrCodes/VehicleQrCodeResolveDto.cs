namespace AutoStock.Services.Dtos.VehicleQrCodes
{
    public class VehicleQrCodeResolveDto
    {
        public string Code { get; set; } = string.Empty;

        public int VehicleId { get; set; }

        public int CustomerId { get; set; }

        public string Plate { get; set; } = string.Empty;

        public string CustomerName { get; set; } = string.Empty;

        public string? VehicleBrandName { get; set; }

        public string? VehicleModelName { get; set; }

        public int? OpenServiceRecordId { get; set; }

        public string? OpenServiceRecordNumber { get; set; }

        public bool HasOpenServiceRecord => OpenServiceRecordId.HasValue;
    }
}