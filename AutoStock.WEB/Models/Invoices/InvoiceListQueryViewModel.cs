using AutoStock.Repositories.Enums;

namespace AutoStock.WEB.Models.Invoices
{
    public class InvoiceListQueryViewModel
    {
        public string? Search { get; set; }

        public InvoiceStatus? Status { get; set; } = InvoiceStatus.Draft;

        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 10;

        public void Normalize()
        {
            Search = Search?.Trim();

            if (PageNumber < 1)
                PageNumber = 1;

            if (PageSize < 1)
                PageSize = 10;

            if (PageSize > 50)
                PageSize = 50;
        }
    }
}