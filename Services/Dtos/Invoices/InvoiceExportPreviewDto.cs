namespace AutoStock.Services.Dtos.Invoices;

public class InvoiceExportPreviewDto
{
    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public string PeriodText { get; set; } = null!;

    public bool IncludeCancelled { get; set; }

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
}
