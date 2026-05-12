namespace AutoStock.Services.Dtos.VehicleQrCodes
{
    public class AssignVehicleQrCodeRequest
    {
        public string Code { get; set; } = null!;

        public int VehicleId { get; set; }
    }
}