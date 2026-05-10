namespace AutoStock.Services.Dtos.ServiceRecords;

public class UpdateServiceRequestItemRequest
{
    public string? RepairDetail { get; set; }

    public decimal? FinalAmount { get; set; }

    public bool IsResolved { get; set; }
}