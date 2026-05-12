namespace AutoStock.WEB.Models.ServiceRecords
{
    public class CreateServiceRecordViewModel
    {

        public string CustomerPhoneNumber { get; set; } = null!;

        public string CustomerName { get; set; } = null!;
        public string? CustomerEmail { get; set; }

        public string Plate { get; set; } = null!;

        public int? VehicleBrandId { get; set; }

        public int? VehicleModelId { get; set; }

        public int? ModelYear { get; set; }

        public int? Mileage { get; set; }

        public string? CustomerComplaint { get; set; }

        public string? ServiceReceptionNote { get; set; }
        public decimal? EstimatedAmount { get; set; }

        public string? EstimatedAmountNote { get; set; }
        public string ServiceAdvisorName { get; set; } = string.Empty;

        public List<CreateServiceRequestItemViewModel> RequestItems { get; set; } = new();

        public List<VehicleBrandViewModel> Brands { get; set; } = new();
    }
}
