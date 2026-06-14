namespace AutoStock.Services.Dtos.Invoices;

public class InvoiceExportFileDto
{
    public string FileName { get; set; } = null!;

    public string ContentType { get; set; } = "application/zip";

    public byte[] Content { get; set; } = Array.Empty<byte>();
}
