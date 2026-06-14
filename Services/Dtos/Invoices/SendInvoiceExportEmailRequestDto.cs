namespace AutoStock.Services.Dtos.Invoices;

public class SendInvoiceExportEmailRequestDto : InvoiceExportQueryDto
{
    public string ToEmail { get; set; } = null!;

    public string? Message { get; set; }
}
