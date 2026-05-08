namespace AutoStock.Services.Dtos.ServiceRecords;

public class CreateServiceRequestItemDto
{
    public string Title { get; set; } = null!;

    public string? Note { get; set; }

    public decimal? EstimatedAmount { get; set; }
}