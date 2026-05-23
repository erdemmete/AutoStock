namespace AutoStock.Services.Dtos.StockItems
{
    public class StockTransactionDto
    {
        public decimal Quantity { get; set; }

        public decimal? UnitPrice { get; set; }

        public string? Description { get; set; }
    }
}