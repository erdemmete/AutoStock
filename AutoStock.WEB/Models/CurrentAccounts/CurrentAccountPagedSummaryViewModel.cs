using AutoStock.WEB.Models.Common;

namespace AutoStock.WEB.Models.CurrentAccounts
{
    public class CurrentAccountPagedSummaryViewModel
    {
        public decimal ThisMonthInvoiceTotal { get; set; }

        public decimal ThisMonthPaymentTotal { get; set; }

        public decimal TotalReceivableBalance { get; set; }

        public PagedResultViewModel<CustomerBalanceSummaryViewModel> CustomerBalances { get; set; } = new();
    }
}