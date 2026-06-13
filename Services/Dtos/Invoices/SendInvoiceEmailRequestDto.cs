namespace AutoStock.Services.Dtos.Invoices;

public class SendInvoiceEmailRequestDto
{
    public string? ToEmail { get; set; }

    public string? Message { get; set; }
}
