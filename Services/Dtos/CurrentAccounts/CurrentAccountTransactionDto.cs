namespace AutoStock.Services.Dtos.CurrentAccounts
{
    public class CurrentAccountTransactionDto
    {
        public int Id { get; set; }

        public DateTime TransactionDate { get; set; }

        public int Type { get; set; }

        public string TypeText { get; set; } = null!;

        public string Description { get; set; } = null!;

        public string? DocumentNumber { get; set; }

        public decimal Debit { get; set; }

        public decimal Credit { get; set; }

        public decimal Balance { get; set; }

        public bool IsSystemGenerated { get; set; }
    }
}