using AutoStock.Repositories.Enums;

namespace AutoStock.Services.Dtos.Customers
{
    public class CustomerDetailDto
    {
        public int Id { get; set; }
        public int WorkshopId { get; set; }

        public CustomerType Type { get; set; }

        public string DisplayName { get; set; } = "Müşteri";

        public string PhoneNumber { get; set; } = string.Empty;

        public string? FullName { get; set; }
        public string? CompanyName { get; set; }
        public string? AuthorizedPersonName { get; set; }

        public string? Email { get; set; }

        public string? TaxOffice { get; set; }
        public string? TaxNumber { get; set; }
        public string? NationalIdentityNumber { get; set; }

        public string? Address { get; set; }
        public string? AddressCity { get; set; }
        public string? AddressDistrict { get; set; }

        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        public decimal Balance { get; set; }
        public int VehicleCount { get; set; }
        public int ServiceRecordCount { get; set; }
        public int InvoiceCount { get; set; }
        public DateTime? LastServiceDate { get; set; }

        public List<CustomerVehicleSummaryDto> Vehicles { get; set; } = new();
        public List<CustomerServiceRecordSummaryDto> RecentServiceRecords { get; set; } = new();
        public List<CustomerInvoiceSummaryDto> RecentInvoices { get; set; } = new();
        public List<CustomerCurrentAccountSummaryDto> RecentCurrentAccountMovements { get; set; } = new();
    }

    public class CustomerVehicleSummaryDto
    {
        public int Id { get; set; }
        public string Plate { get; set; } = null!;
        public string? BrandName { get; set; }
        public string? ModelName { get; set; }
        public int? ModelYear { get; set; }
        public int? Mileage { get; set; }
        public string? ChassisNumber { get; set; }
        public bool IsActive { get; set; }
        public int ServiceRecordCount { get; set; }
        public DateTime? LastServiceDate { get; set; }
    }

    public class CustomerServiceRecordSummaryDto
    {
        public int Id { get; set; }
        public string RecordNumber { get; set; } = null!;
        public string StatusText { get; set; } = null!;
        public string VehiclePlate { get; set; } = null!;
        public string? VehicleBrandName { get; set; }
        public string? VehicleModelName { get; set; }
        public string? ComplaintTitle { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    public class CustomerInvoiceSummaryDto
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; } = null!;
        public string StatusText { get; set; } = null!;
        public string? Plate { get; set; }
        public decimal GrandTotal { get; set; }
        public DateTime InvoiceDate { get; set; }
        public CustomerOfficialInvoiceSummaryDto? OfficialInvoice { get; set; }
    }

    public class CustomerOfficialInvoiceSummaryDto
    {
        public int Id { get; set; }
        public string OfficialInvoiceNumber { get; set; } = null!;
        public DateTime OfficialInvoiceDate { get; set; }
        public DateTime UploadedAt { get; set; }
        public string ShareToken { get; set; } = null!;
        public DateTime? CustomerDeliveredAt { get; set; }
    }

    public class CustomerCurrentAccountSummaryDto
    {
        public int Id { get; set; }
        public string TypeText { get; set; } = null!;
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public decimal BalanceEffect => Debit - Credit;
        public DateTime TransactionDate { get; set; }
        public string Description { get; set; } = null!;
        public string? DocumentNumber { get; set; }
    }
}
