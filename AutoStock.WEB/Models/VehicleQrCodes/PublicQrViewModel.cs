namespace AutoStock.WEB.Models.Qr
{
    public class PublicQrViewModel
    {
        public string Code { get; set; } = null!;

        public string? WorkshopName { get; set; }

        public PublicQrVehicleViewModel Vehicle { get; set; } = new();

        public List<PublicQrServiceRecordViewModel> Records { get; set; } = new();
    }

    public class PublicQrVehicleViewModel
    {
        public string? Plate { get; set; }

        public string? Brand { get; set; }

        public string? Model { get; set; }

        public int? ModelYear { get; set; }
    }

    public class PublicQrServiceRecordViewModel
    {
        public int Id { get; set; }

        public string? RecordNumber { get; set; }

        public DateTime ServiceDate { get; set; }

        public List<string> Items { get; set; } = new();
    }
}
