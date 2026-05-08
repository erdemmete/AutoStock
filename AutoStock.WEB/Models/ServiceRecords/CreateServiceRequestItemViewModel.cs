namespace AutoStock.WEB.Models.ServiceRecords;

public class CreateServiceRequestItemViewModel
{
    public string Title { get; set; } = null!;

    public string? Note { get; set; }

    public decimal? EstimatedAmount { get; set; }
}