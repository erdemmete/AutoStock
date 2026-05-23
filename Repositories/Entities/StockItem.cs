namespace AutoStock.Repositories.Entities
{
    public class StockItem
    {
        public int Id { get; set; }

        public int WorkshopId { get; set; }

        public string Name { get; set; } = null!;

        public string? Code { get; set; }

        public string? Barcode { get; set; }

        public string? Brand { get; set; }

        public string Unit { get; set; } = "Adet";

        public decimal Quantity { get; set; }

        public decimal PurchasePrice { get; set; }

        public decimal SalePrice { get; set; }

        public decimal MinimumQuantity { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<StockMovement> Movements { get; set; } = new List<StockMovement>();
    }
}