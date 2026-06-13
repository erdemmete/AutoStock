namespace AutoStock.Services.Dtos.Invoices;

public class SendInvoiceEmailResponseDto
{
    public int InvoiceId { get; set; }

    public string InvoiceNumber { get; set; } = null!;

    public string ToEmail { get; set; } = null!;
}
