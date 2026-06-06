using AutoStock.WEB.Models.Common;
using AutoStock.WEB.Models.StockItems;

namespace AutoStock.WEB.Services
{
    public class StockItemPageService
    {
        private readonly StockItemApiService _stockItemApiService;

        public StockItemPageService(StockItemApiService stockItemApiService)
        {
            _stockItemApiService = stockItemApiService;
        }

        public async Task<PageViewResult<StockItemIndexViewModel>> BuildIndexAsync(
            StockItemListQueryViewModel? query)
        {
            query ??= new StockItemListQueryViewModel();
            query.Normalize();

            var stockResult = await _stockItemApiService.GetListAsync(query);
            var filterOptionsResult = await _stockItemApiService.GetFilterOptionsAsync();

            var errors = new List<string>();

            if (stockResult.IsFailure)
            {
                errors.Add(stockResult.ErrorMessage ?? "Stok listesi alınırken hata oluştu.");
            }

            if (filterOptionsResult.IsFailure)
            {
                errors.Add(filterOptionsResult.ErrorMessage ?? "Stok filtreleri alınırken hata oluştu.");
            }

            var model = new StockItemIndexViewModel
            {
                Query = query,
                Stocks = stockResult.Data ?? new PagedResultViewModel<StockItemListViewModel>
                {
                    PageNumber = query.PageNumber,
                    PageSize = query.PageSize
                },
                FilterOptions = filterOptionsResult.Data ?? new StockItemFilterOptionsViewModel()
            };

            return errors.Any()
                ? PageViewResult<StockItemIndexViewModel>.WithErrors(model, errors)
                : PageViewResult<StockItemIndexViewModel>.Success(model);
        }
    }
}