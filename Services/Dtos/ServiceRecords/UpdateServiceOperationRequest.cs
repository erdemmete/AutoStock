using AutoStock.Repositories.Enums;

namespace AutoStock.Services.Dtos.ServiceRecords;

public class UpdateServiceOperationRequest
{
    public OperationType Type { get; set; }

    public string Description { get; set; } = null!;

    public int Quantity { get; set; } = 1;

    public decimal UnitPrice { get; set; }

    public string? Note { get; set; }

    public int? ServiceRequestItemId { get; set; }

    public int? StockItemId { get; set; }
}