using System.ComponentModel.DataAnnotations;
using FuelLevelEnum = AutoStock.Repositories.Enums.FuelLevel;

namespace AutoStock.WEB.Models.ServiceRecords
{
    public class CreateServiceRecordViewModel
    {
        public string? ClientRequestId { get; set; }

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

        public FuelLevelEnum? FuelLevel { get; set; }
        public string? ChassisNumber { get; set; }
        public int? VehicleVariantId { get; set; }

        public string? FuelType { get; set; }

        public string? TransmissionType { get; set; }

        public string? BodyType { get; set; }

        public int? EngineCapacityCc { get; set; }

        public int? EnginePowerHp { get; set; }

        public string? EngineCode { get; set; }

        public string? CustomerComplaint { get; set; }

        public string? ServiceReceptionNote { get; set; }
        public decimal? EstimatedAmount { get; set; }

        public string? EstimatedAmountNote { get; set; }
        public string? ServiceAdvisorName { get; set; } = string.Empty;

        public int CustomerType { get; set; } = 1;
        public string? CompanyName { get; set; }
        public string? AuthorizedPersonName { get; set; }
        public string? TaxNumber { get; set; }
        public string? TaxOffice { get; set; }
        public string? CustomerAddress { get; set; }
        public string? NationalIdentityNumber { get; set; }

        public string? AddressCity { get; set; }

        public string? AddressDistrict { get; set; }
        public string? VehicleDeliveredBy { get; set; }
        public string? WorkshopDisplayName { get; set; }

        public string? WorkshopAddressText { get; set; }

        public string? WorkshopPhoneText { get; set; }


        public List<CreateServiceRequestItemViewModel> RequestItems { get; set; } = new();

        public List<VehicleBrandViewModel> Brands { get; set; } = new();
    }
}
