using AutoStock.WEB.Models.Common;

namespace AutoStock.WEB.Models.Invoices
{
    public class InvoiceIndexViewModel
    {
        public InvoiceListQueryViewModel Query { get; set; } = new();

        public PagedResultViewModel<InvoiceListItemViewModel> Invoices { get; set; } = new();
    }
}