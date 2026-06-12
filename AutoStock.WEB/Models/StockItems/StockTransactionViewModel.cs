namespace AutoStock.WEB.Models.StockItems
{
    public class StockTransactionViewModel
    {
        public decimal Quantity { get; set; }

        public decimal? UnitPrice { get; set; }

        public string? Description { get; set; }
    }
}