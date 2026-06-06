using AutoStock.WEB.Models.Common;

namespace AutoStock.WEB.Models.StockItems
{
    public class StockItemIndexViewModel
    {
        public StockItemListQueryViewModel Query { get; set; } = new();

        public PagedResultViewModel<StockItemListViewModel> Stocks { get; set; } = new();

        public StockItemFilterOptionsViewModel FilterOptions { get; set; } = new();
    }
}