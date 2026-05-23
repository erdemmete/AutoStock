namespace AutoStock.WEB.Models.StockItems
{
    public class StockItemDetailViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Code { get; set; }
        public string? Barcode { get; set; }
        public string? Brand { get; set; }
        public string Unit { get; set; } = null!;
        public decimal Quantity { get; set; }
        public decimal PurchasePrice { get; set; }
        public decimal SalePrice { get; set; }
        public decimal MinimumQuantity { get; set; }
        public DateTime CreatedAt { get; set; }

        public List<StockMovementListViewModel> Movements { get; set; } = new();
    }
}