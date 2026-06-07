using AutoStock.Repositories.Enums;

namespace AutoStock.Repositories.Entities
{
    public class StockMovement
    {
        public int Id { get; set; }

        public int WorkshopId { get; set; }

        public int StockItemId { get; set; }

        public StockMovementType MovementType { get; set; }

        public decimal Quantity { get; set; }

        public decimal? UnitPrice { get; set; }

        public string? Description { get; set; }

        public string? ReferenceType { get; set; }

        public int? ReferenceId { get; set; }

        public DateTime CreatedAt { get; set; }

        public int? CreatedByUserId { get; set; }

        public StockItem StockItem { get; set; } = null!;
    }
}