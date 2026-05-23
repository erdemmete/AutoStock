namespace Services.DTOs.StockItems
{
    public class CreateStockItemDto
    {
        public string Name { get; set; } = null!;
        public string? Code { get; set; }
        public string? Barcode { get; set; }
        public string? Brand { get; set; }
        public string Unit { get; set; } = "Adet";
        public decimal Quantity { get; set; }
        public decimal PurchasePrice { get; set; }
        public decimal SalePrice { get; set; }
        public decimal MinimumQuantity { get; set; }
    }
}