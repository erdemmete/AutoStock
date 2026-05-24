namespace AutoStock.Services.Dtos.StockItems;

public class StockItemSelectDto
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Code { get; set; }

    public decimal Quantity { get; set; }

    public decimal SalePrice { get; set; }
}