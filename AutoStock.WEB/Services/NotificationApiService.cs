using AutoStock.Services.Dtos.Common;
using AutoStock.Services.Dtos.Notifications;
using AutoStock.WEB.Models.Common;

namespace AutoStock.WEB.Services
{
    public class NotificationApiService : BaseApiService
    {
        public NotificationApiService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor,
            ILogger<NotificationApiService> logger)
            : base(httpClientFactory, configuration, httpContextAccessor, logger)
        {
        }

        public async Task<ApiResponse<NotificationHeaderDto>> GetHeaderAsync(int maxItems = 8)
        {
            return await GetAsync<NotificationHeaderDto>(
                $"/api/notifications/header?maxItems={maxItems}",
                "Bildirimler alınırken hata oluştu.");
        }

        public async Task<ApiResponse<PagedResult<NotificationListItemDto>>> GetPagedAsync(NotificationListQueryDto query)
        {
            query ??= new NotificationListQueryDto();
            query.Normalize();

            var url = BuildUrlWithQuery("/api/notifications", new Dictionary<string, string?>
            {
                ["pageNumber"] = query.PageNumber.ToString(),
                ["pageSize"] = query.PageSize.ToString(),
                ["isRead"] = query.IsRead?.ToString().ToLowerInvariant()
            });

            return await GetAsync<PagedResult<NotificationListItemDto>>(
                url,
                "Bildirimler alınırken hata oluştu.");
        }

        public async Task<ApiResponse<bool>> MarkAsReadAsync(int userNotificationId)
        {
            return await PostEmptyAsync<bool>(
                $"/api/notifications/{userNotificationId}/read",
                "Bildirim okundu olarak işaretlenirken hata oluştu.");
        }

        public async Task<ApiResponse<bool>> MarkAllAsReadAsync()
        {
            return await PostEmptyAsync<bool>(
                "/api/notifications/read-all",
                "Bildirimler okundu olarak işaretlenirken hata oluştu.");
        }
    }
}