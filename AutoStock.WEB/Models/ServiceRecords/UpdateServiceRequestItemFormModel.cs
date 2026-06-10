namespace AutoStock.WEB.Models.ServiceRecords;

public class UpdateServiceRequestItemFormModel
{
    public int ServiceRequestItemId { get; set; }

    public string Title { get; set; } = null!;

    public string? Note { get; set; }

    public decimal? EstimatedAmount { get; set; }
}