using AutoStock.Services.Dtos.Common;
using AutoStock.Services.Dtos.Emails;

namespace AutoStock.Services.Interfaces
{
    public interface IEmailSender
    {
        Task<ServiceResult<bool>> SendAsync(
            EmailMessageDto message,
            CancellationToken cancellationToken = default);
    }
}