namespace AutoStock.WEB.Models.CurrentAccounts
{
    public class CurrentAccountTransactionViewModel
    {
        public int Id { get; set; }

        public DateTime TransactionDate { get; set; }

        public int Type { get; set; }

        public string TypeText { get; set; } = null!;

        public string Description { get; set; } = null!;

        public string? DocumentNumber { get; set; }

        public int? InvoiceId { get; set; }

        public string? InvoiceNumber { get; set; }

        public decimal Debit { get; set; }

        public decimal Credit { get; set; }

        public decimal Balance { get; set; }

        public bool IsSystemGenerated { get; set; }

        public bool CanCancelPayment { get; set; }

        public bool IsPaymentCancelled { get; set; }

        public bool IsPaymentCancellation { get; set; }
    }
}
