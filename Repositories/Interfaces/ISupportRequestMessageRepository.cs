using AutoStock.Repositories.Entities;

namespace AutoStock.Repositories.Interfaces
{
    public interface ISupportRequestMessageRepository
    {
        Task AddAsync(SupportRequestMessage message);

        Task<List<SupportRequestMessage>> GetBySupportRequestIdAsync(int supportRequestId);
    }
}
