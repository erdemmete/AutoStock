using AutoStock.WEB.Models.Common;
using AutoStock.WEB.Models.Customers;

namespace AutoStock.WEB.Services
{
    public class CustomerApiService : BaseApiService
    {
        public CustomerApiService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor,
            ILogger<CustomerApiService> logger)
            : base(httpClientFactory, configuration, httpContextAccessor, logger)
        {
        }

        public async Task<ApiResponse<PagedResultViewModel<CustomerListItemViewModel>>> GetListAsync(
            CustomerListQueryViewModel query)
        {
            query.Normalize();

            var url = BuildUrlWithQuery("/api/Customers", new Dictionary<string, string?>
            {
                ["search"] = query.Search,
                ["type"] = query.Type.HasValue ? ((int)query.Type.Value).ToString() : null,
                ["pageNumber"] = query.PageNumber.ToString(),
                ["pageSize"] = query.PageSize.ToString()
            });

            return await GetAsync<PagedResultViewModel<CustomerListItemViewModel>>(
                url,
                "Müşteri listesi alınırken hata oluştu.");
        }

        public async Task<ApiResponse<CustomerDetailViewModel>> GetByIdAsync(int id)
        {
            return await GetAsync<CustomerDetailViewModel>(
                $"/api/Customers/{id}",
                "Müşteri bilgileri alınırken hata oluştu.");
        }

        public async Task<ApiResponse<EditCustomerViewModel>> GetEditModelAsync(int id)
        {
            return await GetAsync<EditCustomerViewModel>(
                $"/api/Customers/{id}",
                "Müşteri düzenleme bilgileri alınırken hata oluştu.");
        }

        public async Task<ApiResponse<object>> CreateAsync(CreateCustomerViewModel model)
        {
            return await PostJsonAsync<CreateCustomerViewModel, object>(
                "/api/Customers",
                model,
                "Müşteri oluşturulurken hata oluştu.");
        }

        public async Task<ApiResponse<object>> UpdateAsync(EditCustomerViewModel model)
        {
            return await PutJsonAsync<EditCustomerViewModel, object>(
                $"/api/Customers/{model.Id}",
                model,
                "Müşteri güncellenirken hata oluştu.");
        }

        public async Task<ApiResponse<object>> SetPassiveAsync(int id)
        {
            return await PostEmptyAsync<object>(
                $"/api/Customers/{id}/passive",
                "Müşteri pasifleştirilirken hata oluştu.");
        }
    }
}