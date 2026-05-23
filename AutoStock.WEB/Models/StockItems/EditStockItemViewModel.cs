namespace AutoStock.WEB.Models.StockItems
{
    public class EditStockItemViewModel
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!;

        public string? Code { get; set; }

        public string? Barcode { get; set; }

        public string? Brand { get; set; }

        public string Unit { get; set; } = "Adet";

        public decimal PurchasePrice { get; set; }

        public decimal SalePrice { get; set; }

        public decimal MinimumQuantity { get; set; }
    }
}