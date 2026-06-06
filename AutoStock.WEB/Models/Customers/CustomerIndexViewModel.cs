using AutoStock.WEB.Models.Common;

namespace AutoStock.WEB.Models.Customers
{
    public class CustomerIndexViewModel
    {
        public CustomerListQueryViewModel Query { get; set; } = new();

        public PagedResultViewModel<CustomerListItemViewModel> Customers { get; set; } = new();
    }
}