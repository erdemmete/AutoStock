namespace AutoStock.WEB.Models.ServiceRecords;

public class UpdateServiceRequestItemViewModel
{
    public int RequestItemId { get; set; }

    public int ServiceRecordId { get; set; }

    public string? RepairDetail { get; set; }

    public decimal? FinalAmount { get; set; }

    public bool IsResolved { get; set; }
}