namespace AutoStock.WEB.Models.CurrentAccounts
{
    public class CustomerCurrentAccountViewModel
    {
        public int CustomerId { get; set; }

        public string CustomerName { get; set; } = null!;

        public decimal Balance { get; set; }

        public decimal InvoiceTotal { get; set; }

        public decimal PaymentTotal { get; set; }

        public decimal OpenInvoiceTotal { get; set; }

        public int OpenInvoiceCount { get; set; }

        public DateTime? LastPaymentDate { get; set; }

        public List<CurrentAccountOpenInvoiceViewModel> OpenInvoices { get; set; } = new();

        public List<CurrentAccountTransactionViewModel> Transactions { get; set; } = new();
    }
}
