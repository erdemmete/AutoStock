using AutoStock.Services.Dtos.Common;
using AutoStock.WEB.Models.Common;
using AutoStock.WEB.Models.SupportRequests;
using AutoStock.Services.Dtos.Common;

namespace AutoStock.WEB.Services
{
    public class SupportRequestApiService : BaseApiService
    {
        public SupportRequestApiService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor,
            ILogger<SupportRequestApiService> logger)
            : base(httpClientFactory, configuration, httpContextAccessor, logger)
        {
        }

        public async Task<ApiResponse<PagedResult<SupportRequestListItemViewModel>>> GetPagedAsync(
            SupportRequestListQueryViewModel query)
        {
            var url = BuildSupportRequestListUrl("/api/SupportRequests", query);

            return await GetAsync<PagedResult<SupportRequestListItemViewModel>>(
                url,
                "Destek talepleri alınırken hata oluştu.");
        }

        public async Task<ApiResponse<SupportRequestDetailViewModel>> GetByIdAsync(int id)
        {
            return await GetAsync<SupportRequestDetailViewModel>(
                $"/api/SupportRequests/{id}",
                "Destek talebi detayı alınırken hata oluştu.");
        }

        public async Task<ApiResponse<int>> CreateIssueAsync(CreateIssueSupportRequestViewModel model)
        {
            return await PostJsonAsync<CreateIssueSupportRequestViewModel, int>(
                "/api/SupportRequests/issue",
                model,
                "Destek talebi oluşturulurken hata oluştu.");
        }

        public async Task<ApiResponse<int>> CreateUserRequestAsync(CreateUserSupportRequestViewModel model)
        {
            return await PostJsonAsync<CreateUserSupportRequestViewModel, int>(
                "/api/SupportRequests/user-create-request",
                model,
                "Kullanıcı ekleme talebi oluşturulurken hata oluştu.");
        }

        public async Task<ApiResponse<int>> CancelAsync(int id)
        {
            return await PostEmptyAsync<int>(
                $"/api/SupportRequests/{id}/cancel",
                "Destek talebi iptal edilirken hata oluştu.");
        }

        public async Task<ApiResponse<PagedResult<SupportRequestListItemViewModel>>> GetPagedForAdminAsync(
            AdminSupportRequestListQueryViewModel query)
        {
            var url = BuildAdminSupportRequestListUrl("/api/AdminSupportRequests", query);

            return await GetAsync<PagedResult<SupportRequestListItemViewModel>>(
                url,
                "Destek talepleri alınırken hata oluştu.");
        }

        public async Task<ApiResponse<SupportRequestDetailViewModel>> GetByIdForAdminAsync(int id)
        {
            return await GetAsync<SupportRequestDetailViewModel>(
                $"/api/AdminSupportRequests/{id}",
                "Destek talebi detayı alınırken hata oluştu.");
        }

        public async Task<ApiResponse<int>> AnswerAsync(AdminAnswerSupportRequestViewModel model)
        {
            return await PostJsonAsync<AdminAnswerSupportRequestViewModel, int>(
                $"/api/AdminSupportRequests/{model.Id}/answer",
                model,
                "Destek talebi yanıtlanırken hata oluştu.");
        }

        public async Task<ApiResponse<int>> UpdateStatusAsync(AdminUpdateSupportRequestStatusViewModel model)
        {
            return await PostJsonAsync<AdminUpdateSupportRequestStatusViewModel, int>(
                $"/api/AdminSupportRequests/{model.Id}/status",
                model,
                "Destek talebi durumu güncellenirken hata oluştu.");
        }

        private static string BuildSupportRequestListUrl(
            string baseUrl,
            SupportRequestListQueryViewModel query)
        {
            query ??= new SupportRequestListQueryViewModel();

            return BuildUrlWithQuery(baseUrl, new Dictionary<string, string?>
            {
                ["pageNumber"] = query.PageNumber.ToString(),
                ["pageSize"] = query.PageSize.ToString(),
                ["status"] = query.Status.HasValue ? ((int)query.Status.Value).ToString() : null,
                ["requestType"] = query.RequestType.HasValue ? ((int)query.RequestType.Value).ToString() : null,
                ["search"] = query.Search,
                ["startDate"] = query.StartDate?.ToString("yyyy-MM-dd"),
                ["endDate"] = query.EndDate?.ToString("yyyy-MM-dd")
            });
        }

        private static string BuildAdminSupportRequestListUrl(
            string baseUrl,
            AdminSupportRequestListQueryViewModel query)
        {
            query ??= new AdminSupportRequestListQueryViewModel();

            var url = BuildSupportRequestListUrl(baseUrl, query);

            if (!query.WorkshopId.HasValue)
                return url;

            var separator = url.Contains('?') ? "&" : "?";

            return $"{url}{separator}workshopId={query.WorkshopId.Value}";
        }
    }
}