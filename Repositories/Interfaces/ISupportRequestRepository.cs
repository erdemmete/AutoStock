using AutoStock.Repositories.Entities;
using AutoStock.Repositories.Enums;

namespace AutoStock.Repositories.Interfaces
{
    public interface ISupportRequestRepository
    {
        Task<SupportRequest?> GetByIdAsync(int id);

        Task<SupportRequest?> GetByIdForWorkshopAsync(
    int id,
    int workshopId,
    int? createdByUserId = null);

        Task<List<SupportRequest>> GetListForWorkshopAsync(
    int workshopId,
    SupportRequestStatus? status,
    SupportRequestType? requestType,
    string? search,
    DateTime? startDate,
    DateTime? endDate,
    int page,
    int pageSize,
    int? createdByUserId = null,
    bool excludeClosedAndCancelled = false);

        Task<int> GetCountForWorkshopAsync(
    int workshopId,
    SupportRequestStatus? status,
    SupportRequestType? requestType,
    string? search,
    DateTime? startDate,
    DateTime? endDate,
    int? createdByUserId = null,
    bool excludeClosedAndCancelled = false);

        Task<List<SupportRequest>> GetListForAdminAsync(
            int? workshopId,
            SupportRequestStatus? status,
            SupportRequestType? requestType,
            string? search,
            DateTime? startDate,
            DateTime? endDate,
            int page,
            int pageSize);

        Task<int> GetCountForAdminAsync(
            int? workshopId,
            SupportRequestStatus? status,
            SupportRequestType? requestType,
            string? search,
            DateTime? startDate,
            DateTime? endDate);

        Task AddAsync(SupportRequest supportRequest);

        void Update(SupportRequest supportRequest);

        Task SaveChangesAsync();
    }
}