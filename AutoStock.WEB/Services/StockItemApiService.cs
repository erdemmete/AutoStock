using AutoStock.WEB.Models.Common;
using AutoStock.WEB.Models.StockItems;

namespace AutoStock.WEB.Services
{
    public class StockItemApiService : BaseApiService
    {


        public StockItemApiService(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    IHttpContextAccessor httpContextAccessor,
    ILogger<StockItemApiService> logger)
    : base(httpClientFactory, configuration, httpContextAccessor, logger)
        {
        }

        public async Task<ApiResponse<PagedResultViewModel<StockItemListViewModel>>> GetListAsync(StockItemListQueryViewModel query)
        {
            query.Normalize();

            var url = BuildUrlWithQuery("/api/StockItems", new Dictionary<string, string?>
            {
                ["search"] = query.Search,
                ["brand"] = query.Brand,
                ["criticalOnly"] = query.CriticalOnly ? "true" : null,
                ["pageNumber"] = query.PageNumber.ToString(),
                ["pageSize"] = query.PageSize.ToString()
            });

            return await GetAsync<PagedResultViewModel<StockItemListViewModel>>(
                url,
                "Stok listesi alınırken hata oluştu.");
        }

        public async Task<ApiResponse<object>> CreateAsync(CreateStockItemViewModel model)
        {
            return await PostJsonAsync<CreateStockItemViewModel, object>(
                "/api/StockItems",
                model,
                "Stok kartı oluşturulurken hata oluştu.");
        }

        public async Task<ApiResponse<StockItemDetailViewModel>> GetByIdAsync(int id)
        {
            var detailResult = await GetAsync<StockItemDetailViewModel>(
                $"/api/StockItems/{id}",
                "Stok kartı alınırken hata oluştu.");

            if (detailResult.IsFailure || detailResult.Data == null)
            {
                return detailResult;
            }

            var movementResult = await GetMovementsAsync(id);

            if (movementResult.IsFailure)
            {
                return ApiResponse<StockItemDetailViewModel>.Fail(
                    movementResult.ErrorMessage ?? "Stok hareketleri alınırken hata oluştu.",
                    movementResult.StatusCode,
                    movementResult.TraceId);
            }

            detailResult.Data.Movements = movementResult.Data ?? new List<StockMovementListViewModel>();

            return detailResult;
        }

       public async Task<ApiResponse<List<StockMovementListViewModel>>> GetMovementsAsync(int stockItemId)
{
    return await GetAsync<List<StockMovementListViewModel>>(
        $"/api/StockItems/{stockItemId}/movements",
        "Stok hareketleri alınırken hata oluştu.");
}

        public async Task<ApiResponse<object>> AdjustStockAsync(int stockItemId, AdjustStockViewModel model)
        {
            return await PostJsonAsync<AdjustStockViewModel, object>(
                $"/api/StockItems/{stockItemId}/adjust-stock",
                model,
                "Stok düzeltme işlemi başarısız oldu.");
        }



        public async Task<ApiResponse<EditStockItemViewModel>> GetEditModelAsync(int id)
        {
            var detailResult = await GetByIdAsync(id);

            if (detailResult.IsFailure || detailResult.Data == null)
            {
                return ApiResponse<EditStockItemViewModel>.Fail(
                    detailResult.ErrorMessage ?? "Stok düzenleme bilgileri alınırken hata oluştu.",
                    detailResult.StatusCode,
                    detailResult.TraceId);
            }

            var detail = detailResult.Data;

            var model = new EditStockItemViewModel
            {
                Id = detail.Id,
                Name = detail.Name,
                Code = detail.Code,
                Barcode = detail.Barcode,
                Brand = detail.Brand,
                Unit = detail.Unit,
                PurchasePrice = detail.PurchasePrice,
                SalePrice = detail.SalePrice,
                MinimumQuantity = detail.MinimumQuantity
            };

            return ApiResponse<EditStockItemViewModel>.Success(
                model,
                detailResult.StatusCode);
        }

        public async Task<ApiResponse<object>> UpdateAsync(EditStockItemViewModel model)
        {
            return await PutJsonAsync<EditStockItemViewModel, object>(
                $"/api/StockItems/{model.Id}",
                model,
                "Stok kartı güncellenirken hata oluştu.");
        }

        public async Task<ApiResponse<object>> SetPassiveAsync(int id)
        {
            return await PostEmptyAsync<object>(
                $"/api/StockItems/{id}/passive",
                "Stok kartı silinirken hata oluştu.");
        }
        public async Task<ApiResponse<List<StockItemSelectViewModel>>> GetSelectListAsync()
        {
            return await GetAsync<List<StockItemSelectViewModel>>(
                "/api/StockItems/select-list",
                "Stok seçim listesi alınırken hata oluştu.");
        }

        public async Task<ApiResponse<object>> StockInAsync(int stockItemId, StockTransactionViewModel model)
        {
            return await PostJsonAsync<StockTransactionViewModel, object>(
                $"/api/StockItems/{stockItemId}/stock-in",
                model,
                "Stok girişi yapılırken hata oluştu.");
        }

        public async Task<ApiResponse<StockItemFilterOptionsViewModel>> GetFilterOptionsAsync()
        {
            return await GetAsync<StockItemFilterOptionsViewModel>(
                "/api/StockItems/filter-options",
                "Stok filtre seçenekleri alınırken hata oluştu.");
        }

    }
}
