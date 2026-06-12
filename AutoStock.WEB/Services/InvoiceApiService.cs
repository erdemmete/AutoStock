using AutoStock.Services.Dtos.Invoices;
using AutoStock.Services.Dtos.Vehicles;
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

        public async Task<ApiResponse<InvoiceDetailViewModel>> UpdateAsync(int invoiceId, UpdateInvoiceDto model)
        {
            model.InvoiceId = invoiceId;

            return await PutJsonAsync<UpdateInvoiceDto, InvoiceDetailViewModel>(
                $"/api/Invoices/{invoiceId}",
                model,
                "Fatura güncellenirken hata oluştu.");
        }

        public async Task<ApiResponse<List<InvoiceListItemViewModel>>> GetListByServiceRecordAsync(
    int serviceRecordId)
        {
            return await GetAsync<List<InvoiceListItemViewModel>>(
                $"/api/Invoices/by-service-record/{serviceRecordId}",
                "Servis kaydına ait faturalar alınırken hata oluştu.");
        }

        public async Task<ApiResponse<InvoiceNavigationViewModel>> CreateOrGetDraftFromServiceRecordAsync(
    int serviceRecordId)
        {
            return await PostEmptyAsync<InvoiceNavigationViewModel>(
                $"/api/Invoices/from-service-record/{serviceRecordId}/draft",
                "Fatura hazırlanırken hata oluştu.");
        }

        public async Task<ApiResponse<InvoiceDetailViewModel>> GetDraftByServiceRecordAsync(
            int serviceRecordId)
        {
            return await GetAsync<InvoiceDetailViewModel>(
                $"/api/Invoices/draft/by-service-record/{serviceRecordId}",
                "Taslak fatura bilgisi alınırken hata oluştu.");
        }

        public async Task<ApiResponse<InvoiceNavigationViewModel>> GetActiveInvoiceByServiceRecordAsync(
            int serviceRecordId)
        {
            return await GetAsync<InvoiceNavigationViewModel>(
                $"/api/Invoices/active/by-service-record/{serviceRecordId}",
                "Aktif fatura bilgisi alınırken hata oluştu.");
        }

        public async Task<ApiResponse<List<VehicleBrandDto>>> GetVehicleBrandsAsync()
        {
            return await GetAsync<List<VehicleBrandDto>>(
                "/api/Invoices/vehicle-brands",
                "Araç markaları alınırken hata oluştu.");
        }

        public async Task<ApiResponse<List<VehicleModelDto>>> GetVehicleModelsAsync(int brandId)
        {
            var url = BuildUrlWithQuery(
                "/api/Invoices/vehicle-models",
                new Dictionary<string, string?>
                {
                    ["brandId"] = brandId.ToString()
                });

            return await GetAsync<List<VehicleModelDto>>(
                url,
                "Araç modelleri alınırken hata oluştu.");
        }
    }
}