namespace AutoStock.Repositories.Entities
{
    public class ServiceRequestItem
    {
        public int Id { get; set; }

        public int ServiceRecordId { get; set; }

        public ServiceRecord ServiceRecord { get; set; } = null!;

        public string Title { get; set; } = null!;

        public string? Note { get; set; }

        public string? RepairDetail { get; set; }

        public bool IsResolved { get; set; }

        public decimal? EstimatedAmount { get; set; }

        public decimal? FinalAmount { get; set; }

        public DateTime CreatedAt { get; set; }

        public bool IsDeleted { get; set; }

        public DateTime? DeletedAt { get; set; }

        public ICollection<ServiceOperation> Operations { get; set; } = new List<ServiceOperation>();
    }
}