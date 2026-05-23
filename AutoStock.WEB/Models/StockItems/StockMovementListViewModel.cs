namespace AutoStock.WEB.Models.StockItems
{
    public class StockMovementListViewModel
    {
        public int Id { get; set; }
        public string MovementType { get; set; } = null!;
        public decimal Quantity { get; set; }
        public decimal? UnitPrice { get; set; }
        public string? Description { get; set; }
        public string? ReferenceType { get; set; }
        public int? ReferenceId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}