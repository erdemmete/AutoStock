using AutoStock.Mobile.Models.CurrentAccounts;
using AutoStock.WEB.Models.Common;
using AutoStock.WEB.Models.CurrentAccounts;

namespace AutoStock.WEB.Services
{
    public class CurrentAccountApiService : BaseApiService
    {
        public CurrentAccountApiService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor,
            ILogger<CurrentAccountApiService> logger)
            : base(httpClientFactory, configuration, httpContextAccessor, logger)
        {
        }

        public async Task<ApiResponse<CustomerCurrentAccountViewModel>> GetCustomerAccountAsync(int customerId)
        {
            return await GetAsync<CustomerCurrentAccountViewModel>(
                $"/api/current-accounts/customers/{customerId}",
                "Cari hesap bilgileri alınırken hata oluştu.");
        }

        public async Task<ApiResponse<object>> CreatePaymentAsync(CreatePaymentViewModel model)
        {
            return await PostJsonAsync<CreatePaymentViewModel, object>(
                "/api/current-accounts/payments",
                model,
                "Tahsilat kaydedilirken hata oluştu.");
        }

        public async Task<ApiResponse<object>> CancelPaymentAsync(CancelPaymentViewModel model)
        {
            return await PostJsonAsync<CancelPaymentViewModel, object>(
                $"/api/current-accounts/payments/{model.TransactionId}/cancel",
                model,
                "Tahsilat iptal edilirken hata oluştu.");
        }

        public async Task<ApiResponse<CurrentAccountPagedSummaryViewModel>> GetSummaryAsync(
            CurrentAccountListQueryViewModel query)
        {
            query.Normalize();

            var url = BuildUrlWithQuery("/api/current-accounts/summary", new Dictionary<string, string?>
            {
                ["search"] = query.Search,
                ["pageNumber"] = query.PageNumber.ToString(),
                ["pageSize"] = query.PageSize.ToString()
            });

            return await GetAsync<CurrentAccountPagedSummaryViewModel>(
                url,
                "Cari özet alınırken hata oluştu.");
        }
    }
}
