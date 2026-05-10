namespace AutoStock.WEB.Models.ServiceRecords
{
    public class ServiceOperationViewModel
    {
        public int Id { get; set; }

        public int Type { get; set; }

        public string Description { get; set; } = null!;

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal TotalPrice { get; set; }

        public string? Note { get; set; }
        public int? ServiceRequestItemId { get; set; }
    }
}
