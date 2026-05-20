namespace AutoStock.Services.Dtos.Invoices;

public class InvoiceListItemDto
{
    public int Id { get; set; }

    public int? ServiceRecordId { get; set; }

    public int Type { get; set; }

    public int Status { get; set; }

    public string InvoiceNumber { get; set; } = null!;

    public DateTime InvoiceDate { get; set; }

    public string CustomerTitle { get; set; } = null!;

    public string? Plate { get; set; }

    public decimal GrandTotal { get; set; }
}