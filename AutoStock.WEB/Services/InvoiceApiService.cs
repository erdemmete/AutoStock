using AutoStock.WEB.Models.Common;
using AutoStock.WEB.Models.Invoices;

namespace AutoStock.WEB.Services
{
    public class InvoiceApiService : BaseApiService
    {
        public InvoiceApiService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor,
            ILogger<InvoiceApiService> logger)
            : base(httpClientFactory, configuration, httpContextAccessor, logger)
        {
        }

        public async Task<ApiResponse<PagedResultViewModel<InvoiceListItemViewModel>>> GetListAsync(
            InvoiceListQueryViewModel query)
        {
            query.Normalize();

            var url = BuildUrlWithQuery("/api/Invoices", new Dictionary<string, string?>
            {
                ["search"] = query.Search,
                ["status"] = query.Status.HasValue ? ((int)query.Status.Value).ToString() : null,
                ["pageNumber"] = query.PageNumber.ToString(),
                ["pageSize"] = query.PageSize.ToString()
            });

            return await GetAsync<PagedResultViewModel<InvoiceListItemViewModel>>(
                url,
                "Fatura listesi alınırken hata oluştu.");
        }

        public async Task<ApiResponse<InvoiceCreateViewModel>> GetCreateDraftFromServiceRecordAsync(
            int serviceRecordId)
        {
            return await GetAsync<InvoiceCreateViewModel>(
                $"/api/Invoices/draft/from-service-record/{serviceRecordId}",
                "Fatura taslağı oluşturulurken hata oluştu.");
        }

        public async Task<ApiResponse<InvoiceDetailViewModel>> GetDetailAsync(int invoiceId)
        {
            return await GetAsync<InvoiceDetailViewModel>(
                $"/api/Invoices/{invoiceId}",
                "Fatura detayı alınırken hata oluştu.");
        }

        public async Task<ApiResponse<object>> CreateAsync(InvoiceCreateViewModel model)
        {
            return await PostJsonAsync<InvoiceCreateViewModel, object>(
                "/api/Invoices",
                model,
                "Fatura oluşturulurken hata oluştu.");
        }

        public async Task<ApiResponse<object>> IssueAsync(int invoiceId)
        {
            return await PostEmptyAsync<object>(
                $"/api/Invoices/{invoiceId}/issue",
                "Fatura kesilirken hata oluştu.");
        }

        public async Task<ApiResponse<object>> CancelAsync(int invoiceId)
        {
            return await PostEmptyAsync<object>(
                $"/api/Invoices/{invoiceId}/cancel",
                "Fatura iptal edilirken hata oluştu.");
        }

        public async Task<ApiResponse<InvoiceDetailViewModel>> UpdateAsync(
            int invoiceId,
            InvoiceDetailViewModel model)
        {
            return await PutJsonAsync<InvoiceDetailViewModel, InvoiceDetailViewModel>(
                $"/api/Invoices/{invoiceId}",
                model,
                "Fatura güncellenirken hata oluştu.");
        }
    }
}