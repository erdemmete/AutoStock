namespace AutoStock.Services.Dtos.Emails;

public class EmailAttachmentDto
{
    public string FileName { get; set; } = null!;

    public string ContentType { get; set; } = "application/octet-stream";

    public byte[] Content { get; set; } = Array.Empty<byte>();
}
