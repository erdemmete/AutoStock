namespace AutoStock.Services.Dtos.ServiceRecords;

public class ServiceRequestItemDto
{
    public int Id { get; set; }

    public string Title { get; set; } = null!;

    public string? Note { get; set; }

    public string? RepairDetail { get; set; }

    public decimal? EstimatedAmount { get; set; }

    public decimal? FinalAmount { get; set; }

    public bool IsResolved { get; set; }
}