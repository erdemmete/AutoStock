namespace AutoStock.Services.Dtos.Invoices;

public class InvoiceNavigationDto
{
    public int InvoiceId { get; set; }
    public int Status { get; set; }
    public string InvoiceNumber { get; set; } = null!;
}