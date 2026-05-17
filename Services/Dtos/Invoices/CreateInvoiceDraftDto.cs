namespace AutoStock.Services.Dtos.Invoices
{
    public class CreateInvoiceDraftDto
    {
        public int ServiceRecordId { get; set; }
        public int CustomerId { get; set; }

        public string CustomerTitle { get; set; } = null!;
        public string? CustomerTaxOffice { get; set; }
        public string? CustomerTaxNumber { get; set; }
        public string? CustomerTckn { get; set; }
        public string? CustomerAddress { get; set; }

        public string? Plate { get; set; }
        public string? ChassisNumber { get; set; }
        public int? Mileage { get; set; }

        public List<CreateInvoiceDraftItemDto> Items { get; set; } = new();
    }

    public class CreateInvoiceDraftItemDto
    {
        public int ItemType { get; set; } = 3;

        public string? Description { get; set; }
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = "Adet";
        public decimal UnitPrice { get; set; }
        public decimal DiscountRate { get; set; }
        public decimal VatRate { get; set; } = 20;
    }
}