using AutoStock.Repositories.Enums;

namespace AutoStock.Services.Dtos.ServiceRecords
{
    public class ServiceRecordDetailDto
    {
        public int Id { get; set; }

        public string RecordNumber { get; set; } = null!;

        public ServiceRecordStatus Status { get; set; }

        public string CustomerName { get; set; } = null!;

        public string CustomerPhone { get; set; } = null!;

        public string VehiclePlate { get; set; } = null!;

        public string? VehicleBrandName { get; set; }

        public string? VehicleModelName { get; set; }

        public int? Mileage { get; set; }
        public FuelLevel? FuelLevel { get; set; }

        public string? CustomerComplaint { get; set; }

        public string? ServiceReceptionNote { get; set; }

        public string? RepairNote { get; set; }

        public decimal TotalAmount { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int VehicleId { get; set; }
        public string? ChassisNumber { get; set; }

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


        public List<ServiceRequestItemDto> RequestItems { get; set; } = new();

        public List<ServiceOperationDto> Operations { get; set; } = new();
        public List<ServiceRequestItemDto> DeletedRequestItems { get; set; } = new();
    }
}
