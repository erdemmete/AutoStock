namespace AutoStock.Services.Dtos.VehicleQrCodes
{
    public class AssignVehicleQrCodeRequest
    {
        public string Code { get; set; } = null!;

        public int VehicleId { get; set; }
    }

    public class CreateVehicleQrCodeRequest
    {
        public int VehicleId { get; set; }
    }

    public class VehicleQrCodeActionResultDto
    {
        public int Id { get; set; }

        public string Code { get; set; } = string.Empty;

        public int VehicleId { get; set; }

        public bool ReplacedExistingQr { get; set; }
    }
}
