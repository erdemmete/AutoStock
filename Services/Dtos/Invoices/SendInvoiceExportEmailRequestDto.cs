namespace AutoStock.Services.Dtos.Invoices;

public class SendInvoiceExportEmailRequestDto : InvoiceExportQueryDto
{
    public string ToEmail { get; set; } = null!;

    public string? Message { get; set; }
}

public class SendInvoiceExportEmailResponseDto
{
    public int RequestedCount { get; set; }

    public int SentCount { get; set; }

    public int SkippedCount { get; set; }

    public int FailedCount { get; set; }

    public List<string> Messages { get; set; } = new();

    public string SummaryMessage { get; set; } = string.Empty;
}
