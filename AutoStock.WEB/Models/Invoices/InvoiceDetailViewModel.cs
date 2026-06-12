namespace AutoStock.WEB.Models.Invoices;

public class InvoiceDetailViewModel
{
    public int Id { get; set; }

    public int CustomerId { get; set; }

    public int? ServiceRecordId { get; set; }

    public int Type { get; set; }

    public int Status { get; set; }

    public string InvoiceNumber { get; set; } = null!;

    public DateTime InvoiceDate { get; set; }

    public string CustomerTitle { get; set; } = null!;

    public string? CustomerTaxOffice { get; set; }

    public string? CustomerTaxNumber { get; set; }

    public string? CustomerTckn { get; set; }

    public string? CustomerAddress { get; set; }

    public string? Plate { get; set; }

    public string? ChassisNumber { get; set; }

    public string? CustomerAddressLine { get; set; }
    public string? CustomerAddressCity { get; set; }
    public string? CustomerAddressDistrict { get; set; }

    public int? VehicleBrandId { get; set; }
    public int? VehicleModelId { get; set; }
    public string? VehicleBrandName { get; set; }
    public string? VehicleModelName { get; set; }
    public int? VehicleModelYear { get; set; }

    public int? Mileage { get; set; }

    public decimal Subtotal { get; set; }

    public decimal DiscountTotal { get; set; }

    public decimal VatTotal { get; set; }

    public decimal GrandTotal { get; set; }

    public string? Notes { get; set; }

    public List<InvoiceDetailItemViewModel> Items { get; set; } = new();
    public List<InvoiceServiceRequestItemOptionViewModel> ServiceRequestItems { get; set; } = new();
}

public class InvoiceDetailItemViewModel
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
    public string? Plate { get; set; }

    public string? ChassisNumber { get; set; }

    public string? VehicleBrandName { get; set; }

    public string? VehicleModelName { get; set; }

    public int? VehicleModelYear { get; set; }

    public int? Mileage { get; set; }

    public List<InvoiceServiceRequestItemOptionViewModel> ServiceRequestItems { get; set; } = new();
}

public class InvoiceServiceRequestItemOptionViewModel
{
    public int Id { get; set; }

    public string Title { get; set; } = null!;
}