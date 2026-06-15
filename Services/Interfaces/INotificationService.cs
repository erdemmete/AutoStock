using AutoStock.Services.Dtos.Common;
using AutoStock.Services.Dtos.Notifications;

namespace AutoStock.Services.Interfaces
{
    public interface INotificationService
    {
        Task<ServiceResult<bool>> CreateForUsersAsync(
            IEnumerable<int> userIds,
            CreateNotificationDto request);

        Task<ServiceResult<bool>> CreateForAdminsAsync(CreateNotificationDto request);

        Task<ServiceResult<bool>> CreateForWorkshopOwnersAsync(
            int workshopId,
            CreateNotificationDto request);

        Task<ServiceResult<bool>> CreateForWorkshopOwnersAndUsersAsync(
            int workshopId,
            IEnumerable<int> userIds,
            CreateNotificationDto request);

        Task<ServiceResult<NotificationHeaderDto>> GetHeaderAsync(int userId, int maxItems = 8);

        Task<ServiceResult<PagedResult<NotificationListItemDto>>> GetPagedAsync(
            NotificationListQueryDto query,
            int userId);

        Task<ServiceResult<bool>> MarkAsReadAsync(int userNotificationId, int userId);

        Task<ServiceResult<bool>> MarkAllAsReadAsync(int userId);
    }
}
