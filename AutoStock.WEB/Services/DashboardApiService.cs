using AutoStock.WEB.Models;
using AutoStock.WEB.Models.Common;

namespace AutoStock.WEB.Services
{
    public class DashboardApiService : BaseApiService
    {
        public DashboardApiService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor,
            ILogger<DashboardApiService> logger)
            : base(httpClientFactory, configuration, httpContextAccessor, logger)
        {
        }

        public async Task<ApiResponse<DashboardViewModel>> GetAsync()
        {
            return await GetAsync<DashboardViewModel>(
                "/api/Dashboard",
                "Dashboard bilgileri alınırken hata oluştu.");
        }
    }
}