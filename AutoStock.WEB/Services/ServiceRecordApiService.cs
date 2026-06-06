using AutoStock.Web.Models.ServiceRecords;
using AutoStock.WEB.Models.Common;

namespace AutoStock.WEB.Services
{
    public class ServiceRecordApiService : BaseApiService
    {
        public ServiceRecordApiService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor,
            ILogger<ServiceRecordApiService> logger)
            : base(httpClientFactory, configuration, httpContextAccessor, logger)
        {
        }

        public async Task<ApiResponse<PagedResultViewModel<ServiceRecordListItemViewModel>>> GetPagedAsync(
            ServiceRecordListQueryViewModel query)
        {
            var url = BuildUrlWithQuery(
                "/api/ServiceRecords",
                new Dictionary<string, string?>
                {
                    ["search"] = query.Search,
                    ["statusFilter"] = query.StatusFilter,
                    ["createdFrom"] = query.CreatedFrom?.ToString("yyyy-MM-dd"),
                    ["createdTo"] = query.CreatedTo?.ToString("yyyy-MM-dd"),
                    ["pageNumber"] = query.PageNumber.ToString(),
                    ["pageSize"] = query.PageSize.ToString()
                });

            return await GetAsync<PagedResultViewModel<ServiceRecordListItemViewModel>>(
                url,
                "Servis kayıtları alınırken hata oluştu.");
        }
    }
}