namespace AutoStock.Services.Dtos.CurrentAccounts
{
    public class CustomerBalanceSummaryDto
    {
        public int CustomerId { get; set; }

        public string CustomerName { get; set; } = null!;

        public decimal Balance { get; set; }
    }
}