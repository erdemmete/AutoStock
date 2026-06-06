namespace AutoStock.WEB.Models.CurrentAccounts
{
    public class CurrentAccountIndexViewModel
    {
        public CurrentAccountListQueryViewModel Query { get; set; } = new();

        public CurrentAccountPagedSummaryViewModel Summary { get; set; } = new();
    }
}