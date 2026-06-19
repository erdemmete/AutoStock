using AutoStock.Services.Dtos.WebPush;
using AutoStock.WEB.Models.Common;

namespace AutoStock.WEB.Services
{
    public class WebPushApiService : BaseApiService
    {
        public WebPushApiService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor,
            ILogger<WebPushApiService> logger)
            : base(httpClientFactory, configuration, httpContextAccessor, logger)
        {
        }

        public async Task<ApiResponse<string>> GetPublicKeyAsync()
        {
            return await GetAsync<string>(
                "/api/web-push/public-key",
                "Tarayıcı bildirimi yapılandırması alınamadı.");
        }

        public async Task<ApiResponse<WebPushSubscriptionStatusDto>> GetStatusAsync(WebPushSubscriptionRequestDto request)
        {
            return await PostJsonAsync<WebPushSubscriptionRequestDto, WebPushSubscriptionStatusDto>(
                "/api/web-push/status",
                request,
                "Tarayıcı bildirimi durumu alınamadı.");
        }

        public async Task<ApiResponse<bool>> SubscribeAsync(WebPushSubscriptionRequestDto request)
        {
            return await PostJsonAsync<WebPushSubscriptionRequestDto, bool>(
                "/api/web-push/subscribe",
                request,
                "Tarayıcı bildirimi açılırken hata oluştu.");
        }

        public async Task<ApiResponse<bool>> UnsubscribeAsync(WebPushSubscriptionRequestDto request)
        {
            return await PostJsonAsync<WebPushSubscriptionRequestDto, bool>(
                "/api/web-push/unsubscribe",
                request,
                "Tarayıcı bildirimi kapatılırken hata oluştu.");
        }
    }
}
