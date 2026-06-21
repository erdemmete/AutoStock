namespace AutoStock.Services.Dtos.CurrentAccounts
{
    public class GetCustomerCurrentAccountResponseDto
    {
        public int CustomerId { get; set; }

        public string CustomerName { get; set; } = null!;

        public string? CustomerPhone { get; set; }

        public decimal Balance { get; set; }

        public decimal InvoiceTotal { get; set; }

        public decimal PaymentTotal { get; set; }

        public decimal OpenInvoiceTotal { get; set; }

        public int OpenInvoiceCount { get; set; }

        public DateTime? LastPaymentDate { get; set; }

        public List<CurrentAccountOpenInvoiceDto> OpenInvoices { get; set; } = new();

        public List<CurrentAccountTransactionDto> Transactions { get; set; } = new();
    }
}
