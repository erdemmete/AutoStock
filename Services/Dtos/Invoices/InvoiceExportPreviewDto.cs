namespace AutoStock.Services.Dtos.Invoices;

public class InvoiceExportPreviewDto
{
    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public string PeriodText { get; set; } = null!;

    public bool IncludeCancelled { get; set; }

    public string Tab { get; set; } = "prepare";

    public int PrepareCount { get; set; }

    public int WaitingCount { get; set; }

    public int CustomerShareCount { get; set; }

    public int CompletedCount { get; set; }

    public int InvoiceCount { get; set; }

    public int IssuedInvoiceCount { get; set; }

    public int CancelledInvoiceCount { get; set; }

    public decimal Subtotal { get; set; }

    public decimal VatTotal { get; set; }

    public decimal GrandTotal { get; set; }

    public decimal PaidTotal { get; set; }

    public decimal RemainingTotal { get; set; }

    public List<InvoiceExportItemDto> Items { get; set; } = new();
}

public class InvoiceExportItemDto
{
    public int InvoiceId { get; set; }

    public int? ServiceRecordId { get; set; }

    public string InvoiceNumber { get; set; } = null!;

    public DateTime InvoiceDate { get; set; }

    public string CustomerTitle { get; set; } = null!;

    public string? TaxNumber { get; set; }

    public string? Plate { get; set; }

    public int Status { get; set; }

    public string StatusText { get; set; } = null!;

    public decimal Subtotal { get; set; }

    public decimal VatTotal { get; set; }

    public decimal GrandTotal { get; set; }

    public decimal PaidTotal { get; set; }

    public decimal RemainingAmount { get; set; }

    public int? AccountingRequestId { get; set; }

    public string? BatchToken { get; set; }

    public int? OfficialInvoiceDocumentId { get; set; }

    public string? OfficialInvoiceNumber { get; set; }

    public string? OfficialInvoiceShareToken { get; set; }

    public DateTime? CustomerDeliveredAt { get; set; }

    public string? RecipientEmail { get; set; }

    public DateTime? AccountingSentAt { get; set; }
}
