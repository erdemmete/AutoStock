namespace AutoStock.Services.Dtos.CurrentAccounts
{
    public class GetCustomerCurrentAccountResponseDto
    {
        public int CustomerId { get; set; }

        public string CustomerName { get; set; } = null!;

        public decimal Balance { get; set; }

        public List<CurrentAccountTransactionDto> Transactions { get; set; } = new();
    }
}