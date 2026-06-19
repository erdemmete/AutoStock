using AutoStock.Services.Dtos.Common;
using AutoStock.Services.Dtos.WebPush;

namespace AutoStock.Services.Interfaces
{
    public interface IWebPushSubscriptionService
    {
        Task<ServiceResult<WebPushSubscriptionStatusDto>> GetStatusAsync(
            int userId,
            int? workshopId,
            string? endpoint);

        Task<ServiceResult<string>> GetPublicKeyAsync();

        Task<ServiceResult<bool>> UpsertAsync(
            int userId,
            int? workshopId,
            WebPushSubscriptionRequestDto request);

        Task<ServiceResult<bool>> DisableAsync(
            int userId,
            string endpoint);

        Task<ServiceResult<bool>> DisableAllForUserAsync(int userId);
    }
}
