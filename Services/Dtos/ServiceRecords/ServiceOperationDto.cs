using AutoStock.Repositories.Enums;

public class ServiceOperationDto
{
    public int Id { get; set; }

    public OperationType Type { get; set; }

    public string Description { get; set; } = null!;

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal TotalPrice { get; set; }

    public string? Note { get; set; }

    public int? ServiceRequestItemId { get; set; }

    public int? StockItemId { get; set; }
}