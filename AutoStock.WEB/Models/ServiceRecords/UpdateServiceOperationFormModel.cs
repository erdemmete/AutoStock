namespace AutoStock.WEB.Models.ServiceRecords;

public class UpdateServiceOperationFormModel
{
    public int OperationId { get; set; }

    public int ServiceRecordId { get; set; }

    public int? ServiceRequestItemId { get; set; }

    public int? StockItemId { get; set; }

    public int Type { get; set; }

    public string Description { get; set; } = null!;

    public int Quantity { get; set; } = 1;

    public decimal UnitPrice { get; set; }

    public string? Note { get; set; }
}