namespace AutoStock.WEB.Models.CurrentAccounts
{
    public class CustomerBalanceSummaryViewModel
    {
        public int CustomerId { get; set; }

        public string CustomerName { get; set; } = null!;

        public decimal Balance { get; set; }
    }
}