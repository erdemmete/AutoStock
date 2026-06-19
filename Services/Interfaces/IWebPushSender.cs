using AutoStock.Repositories.Entities;
using AutoStock.Services.Dtos.WebPush;

namespace AutoStock.Services.Interfaces
{
    public interface IWebPushSender
    {
        Task SendToUsersAsync(
            IReadOnlyCollection<int> userIds,
            Notification notification,
            WebPushPayloadDto payload,
            CancellationToken cancellationToken = default);
    }
}
