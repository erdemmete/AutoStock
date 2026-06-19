namespace AutoStock.Services.Dtos.Invoices;

public class InvoiceDetailDto
{
    public int Id { get; set; }

    public int WorkshopId { get; set; }

    public int CustomerId { get; set; }

    public int? ServiceRecordId { get; set; }

    public string? PublicServiceQrCode { get; set; }

    public int Type { get; set; }

    public int Status { get; set; }

    public string InvoiceNumber { get; set; } = null!;

    public DateTime InvoiceDate { get; set; }

    public string? RowVersion { get; set; }

    public string? WorkshopDisplayName { get; set; }

    public string? WorkshopLegalTitle { get; set; }

    public string? WorkshopTaxOffice { get; set; }

    public string? WorkshopTaxNumber { get; set; }

    public string? WorkshopTradeRegistryNumber { get; set; }

    public string? WorkshopMersisNumber { get; set; }

    public string? WorkshopEmail { get; set; }

    public string? WorkshopPhoneNumber { get; set; }

    public string? WorkshopFaxNumber { get; set; }

    public string? WorkshopWebsite { get; set; }

    public string? WorkshopAddressLine { get; set; }

    public string? WorkshopCity { get; set; }

    public string? WorkshopDistrict { get; set; }

    public string? WorkshopPostalCode { get; set; }

    public string? WorkshopCountry { get; set; }

    public string CustomerTitle { get; set; } = null!;

    public string? CustomerTaxOffice { get; set; }

    public string? CustomerTaxNumber { get; set; }

    public string? CustomerTckn { get; set; }

    public string? CustomerAddress { get; set; }

    public string? CustomerEmail { get; set; }

    public string? CustomerAddressLine { get; set; }

    public string? CustomerAddressCity { get; set; }

    public string? CustomerAddressDistrict { get; set; }

    public string? Plate { get; set; }

    public string? ChassisNumber { get; set; }

    public int? Mileage { get; set; }

    public int? VehicleBrandId { get; set; }

    public int? VehicleModelId { get; set; }

    public string? VehicleBrandName { get; set; }

    public string? VehicleModelName { get; set; }

    public int? VehicleModelYear { get; set; }

    public decimal Subtotal { get; set; }

    public decimal DiscountTotal { get; set; }

    public decimal VatTotal { get; set; }

    public decimal GrandTotal { get; set; }

    public string? Notes { get; set; }

    public decimal CustomerBalance { get; set; }

    public decimal InvoicePaidTotal { get; set; }

    public decimal InvoiceRemainingAmount { get; set; }

    public List<InvoiceDetailItemDto> Items { get; set; } = new();

    public List<InvoiceBankAccountDto> BankAccounts { get; set; } = new();
}

public class InvoiceDetailItemDto
{
    public int Id { get; set; }

    public int ItemType { get; set; }

    public string Description { get; set; } = null!;

    public decimal Quantity { get; set; }

    public string Unit { get; set; } = null!;

    public decimal UnitPrice { get; set; }

    public decimal DiscountRate { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal VatRate { get; set; }

    public decimal VatAmount { get; set; }

    public decimal LineTotal { get; set; }

    public int? StockItemId { get; set; }
}
