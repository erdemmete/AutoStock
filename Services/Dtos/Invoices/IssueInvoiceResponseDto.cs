namespace AutoStock.Services.Dtos.Invoices;

public class IssueInvoiceResponseDto
{
    public int InvoiceId { get; set; }

    public int Status { get; set; }

    public string InvoiceNumber { get; set; } = null!;
}