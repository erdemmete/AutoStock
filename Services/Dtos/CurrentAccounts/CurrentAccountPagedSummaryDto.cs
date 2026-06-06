using AutoStock.Services.Dtos.Common;

namespace AutoStock.Services.Dtos.CurrentAccounts
{
    public class CurrentAccountPagedSummaryDto
    {
        public decimal ThisMonthInvoiceTotal { get; set; }

        public decimal ThisMonthPaymentTotal { get; set; }

        public decimal TotalReceivableBalance { get; set; }

        public PagedResult<CustomerBalanceSummaryDto> CustomerBalances { get; set; } = new();
    }
}