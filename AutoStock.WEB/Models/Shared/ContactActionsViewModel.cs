namespace AutoStock.WEB.Models.Shared;

public class ContactActionsViewModel
{
    public string? PhoneNumber { get; set; }

    public string? CustomerName { get; set; }

    public string? Plate { get; set; }

    public string? ServiceStatus { get; set; }

    public string Variant { get; set; } = "compact";

    public string? EditUrl { get; set; }
}
