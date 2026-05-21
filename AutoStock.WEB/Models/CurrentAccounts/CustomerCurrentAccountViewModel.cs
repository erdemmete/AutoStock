

namespace AutoStock.WEB.Models.CurrentAccounts
{
    public class CustomerCurrentAccountViewModel
    {
        public int CustomerId { get; set; }

        public string CustomerName { get; set; } = null!;

        public decimal Balance { get; set; }

        public List<CurrentAccountTransactionViewModel> Transactions { get; set; } = new();
    }
}