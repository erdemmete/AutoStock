namespace AutoStock.Services.Dtos.Invoices;

public class CreateInvoiceResponseDto
{
    public int InvoiceId { get; set; }
    public int ServiceRecordId { get; set; }
    public string? InvoiceNumber { get; set; }
    public decimal GrandTotal { get; set; }
}