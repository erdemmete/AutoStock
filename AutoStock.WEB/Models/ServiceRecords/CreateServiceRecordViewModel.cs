using System.ComponentModel.DataAnnotations;

namespace AutoStock.WEB.Models.ServiceRecords
{
    public class CreateServiceRecordViewModel
    {
        [Required(ErrorMessage = "Telefon numarası girilmesi zorunludur.")]
        public string CustomerPhoneNumber { get; set; } = null!;

        [Required(ErrorMessage = "Ad soyad girilmesi zorunludur.")]
        public string CustomerName { get; set; } = null!;
        public string? CustomerEmail { get; set; }

        [Required(ErrorMessage = "Plaka girilmesi zorunludur.")]
        public string Plate { get; set; } = null!;

        [Required(ErrorMessage = "Araç markası girilmesi zorunludur.")]
        public int? VehicleBrandId { get; set; }

        [Required(ErrorMessage = "Araç modeli girilmesi zorunludur.")]
        public int? VehicleModelId { get; set; }

        public int? ModelYear { get; set; }

        public int? Mileage { get; set; }
        public string? ChassisNumber { get; set; }

        public string? CustomerComplaint { get; set; }

        public string? ServiceReceptionNote { get; set; }
        public decimal? EstimatedAmount { get; set; }

        public string? EstimatedAmountNote { get; set; }
        public string? ServiceAdvisorName { get; set; } = string.Empty;

        public List<CreateServiceRequestItemViewModel> RequestItems { get; set; } = new();

        public List<VehicleBrandViewModel> Brands { get; set; } = new();
    }
}
