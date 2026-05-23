namespace AutoStock.Services.Dtos.StockItems
{
    public class AdjustStockDto
    {
        public decimal NewQuantity { get; set; }
        public string? Description { get; set; }
    }
}