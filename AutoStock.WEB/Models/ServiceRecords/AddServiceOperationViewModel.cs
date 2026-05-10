namespace AutoStock.WEB.Models.ServiceRecords;

public class AddServiceOperationViewModel
{
    public int ServiceRecordId { get; set; }

    public int? ServiceRequestItemId { get; set; }

    public int Type { get; set; }

    public string Description { get; set; } = null!;

    public int Quantity { get; set; } = 1;

    public decimal UnitPrice { get; set; }

    public string? Note { get; set; }
}