namespace AutoStock.Services.Dtos.ServiceRecords;

public class UpdateServiceRequestItemRequest
{
    public string? Title { get; set; }

    public string? Note { get; set; }

    public decimal? EstimatedAmount { get; set; }

    public string? RepairDetail { get; set; }

    public decimal? FinalAmount { get; set; }

    public bool? IsResolved { get; set; }
}