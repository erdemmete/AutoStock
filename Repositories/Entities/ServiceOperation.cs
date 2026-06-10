using AutoStock.Repositories.Enums;

namespace AutoStock.Repositories.Entities
{
    public class ServiceOperation
    {
        public int Id { get; set; }

        public int ServiceRecordId { get; set; }

        public ServiceRecord ServiceRecord { get; set; } = null!;

        public int? StockItemId { get; set; }

        public StockItem? StockItem { get; set; }

        public OperationType Type { get; set; }

        public string Description { get; set; } = null!;

        public int Quantity { get; set; } = 1;

        public decimal UnitPrice { get; set; }

        public decimal TotalPrice { get; set; }

        public string? Note { get; set; }

        public int? ServiceRequestItemId { get; set; }

        public ServiceRequestItem? ServiceRequestItem { get; set; }

        public bool IsDeleted { get; set; }

        public DateTime? DeletedAt { get; set; }
    }
}