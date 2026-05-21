namespace AutoStock.Services.Dtos.CurrentAccounts
{
    public class CurrentAccountSummaryDto
    {
        public decimal ThisMonthInvoiceTotal { get; set; }

        public decimal ThisMonthPaymentTotal { get; set; }

        public decimal TotalReceivableBalance { get; set; }

        public List<CustomerBalanceSummaryDto> CustomerBalances { get; set; } = new();
    }
}