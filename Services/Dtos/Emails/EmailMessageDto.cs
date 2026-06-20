namespace AutoStock.Services.Dtos.Emails;

public class EmailMessageDto
{
    public string ToEmail { get; set; } = null!;

    public string? ToName { get; set; }

    public string? FromName { get; set; }

    public string Subject { get; set; } = null!;

    public string HtmlBody { get; set; } = null!;

    public string? TextBody { get; set; }

    public List<EmailAttachmentDto> Attachments { get; set; } = new();
}
