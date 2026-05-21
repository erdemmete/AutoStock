namespace AutoStock.WEB.Models.CurrentAccounts
{
    public class CurrentAccountSummaryViewModel
    {
        public decimal ThisMonthInvoiceTotal { get; set; }

        public decimal ThisMonthPaymentTotal { get; set; }

        public decimal TotalReceivableBalance { get; set; }

        public List<CustomerBalanceSummaryViewModel> CustomerBalances { get; set; } = new();
    }
}