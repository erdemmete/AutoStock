using AutoStock.Services.Dtos.ServiceRecords;
using AutoStock.WEB.Models.Invoices;
using AutoStock.WEB.Models.StockItems;
using FuelLevelEnum = AutoStock.Repositories.Enums.FuelLevel;

namespace AutoStock.WEB.Models.ServiceRecords
{


    public class ServiceRecordDetailViewModel
    {
        public int Id { get; set; }

        public string RecordNumber { get; set; } = null!;

        public int Status { get; set; }

        public string CustomerName { get; set; } = null!;

        public string CustomerPhone { get; set; } = null!;

        public string VehiclePlate { get; set; } = null!;

        public string? VehicleBrandName { get; set; }

        public string? VehicleModelName { get; set; }

        public string? ChassisNumber { get; set; }

        public int? Mileage { get; set; }
        public FuelLevelEnum? FuelLevel { get; set; }

        public string FuelLevelText => FuelLevel switch
        {
            FuelLevelEnum.Empty => "Boş",
            FuelLevelEnum.Quarter => "1/4",
            FuelLevelEnum.Half => "1/2",
            FuelLevelEnum.ThreeQuarters => "3/4",
            FuelLevelEnum.Full => "Dolu",
            _ => "-"
        };

        public string? CustomerComplaint { get; set; }

        public string? ServiceReceptionNote { get; set; }

        public string? RepairNote { get; set; }

        public decimal TotalAmount { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        public int VehicleId { get; set; }
        public string? VehicleDeliveredBy { get; set; }

        public string? CustomerEmail { get; set; }

        public string? CompanyName { get; set; }

        public string? AuthorizedPersonName { get; set; }

        public string? TaxNumber { get; set; }

        public string? TaxOffice { get; set; }

        public string? NationalIdentityNumber { get; set; }

        public string? AddressCity { get; set; }

        public string? AddressDistrict { get; set; }

        public string? CustomerAddress { get; set; }
        public int? DraftInvoiceId { get; set; }
        public int? ActiveInvoiceId { get; set; }
        public int? ActiveInvoiceStatus { get; set; }
        public string? ActiveInvoiceNumber { get; set; }

        public List<ServiceOperationViewModel> Operations { get; set; } = new();

        public List<ServiceRequestItemViewModel> RequestItems { get; set; } = new();

        public List<InvoiceListItemViewModel> Invoices { get; set; } = new();
        public List<StockItemSelectViewModel> StockItems { get; set; } = new();
        public List<ServiceRequestItemViewModel> DeletedRequestItems { get; set; } = new();
        public List<ServiceRecordImageDto> Images { get; set; } = new();
    }
}
