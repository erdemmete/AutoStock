namespace AutoStock.Services.Dtos.Invoices;

public class InvoiceExportQueryDto
{
    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public string? Preset { get; set; }

    public bool IncludeCancelled { get; set; } = false;
}
