using AutoStock.Repositories.Enums;

namespace AutoStock.Repositories.Entities
{
    public class CurrentAccountTransaction
    {
        public int Id { get; set; }

        public int WorkshopId { get; set; }
        public int CustomerId { get; set; }
        public int? InvoiceId { get; set; }

        public CurrentAccountTransactionType Type { get; set; }

        public decimal Debit { get; set; }
        public decimal Credit { get; set; }

        public DateTime TransactionDate { get; set; } 

        public string Description { get; set; } = null!;
        public string? DocumentNumber { get; set; }

        public bool IsSystemGenerated { get; set; } = false;

        public DateTime CreatedAt { get; set; }

        public Customer Customer { get; set; } = null!;
        public Invoice? Invoice { get; set; }
    }
}