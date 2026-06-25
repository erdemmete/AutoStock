using AutoStock.Services.Dtos.Common;
using AutoStock.Services.Dtos.EditLocks;

namespace AutoStock.Services.Interfaces
{
    public interface IEntityEditLockService
    {
        Task<ServiceResult<EntityEditLockDto>> AcquireAsync(
            string entityType,
            int entityId,
            string? existingLockToken,
            int workshopId,
            int userId,
            CancellationToken cancellationToken = default);

        Task<ServiceResult<EntityEditLockDto>> GetStatusAsync(
            string entityType,
            int entityId,
            int workshopId,
            int userId,
            CancellationToken cancellationToken = default);

        Task<ServiceResult<bool>> HeartbeatAsync(
            string entityType,
            int entityId,
            string? lockToken,
            int workshopId,
            int userId,
            CancellationToken cancellationToken = default);

        Task<ServiceResult<bool>> ReleaseAsync(
            string entityType,
            int entityId,
            string? lockToken,
            int workshopId,
            int userId,
            CancellationToken cancellationToken = default);

        Task<ServiceResult<bool>> ForceReleaseAsync(
            string entityType,
            int entityId,
            int workshopId,
            int userId,
            string? userRole,
            CancellationToken cancellationToken = default);

        Task<ServiceResult<List<AdminEntityEditLockDto>>> GetWorkshopLocksForAdminAsync(
            int workshopId,
            CancellationToken cancellationToken = default);

        Task<ServiceResult<bool>> ForceReleaseForAdminAsync(
            string entityType,
            int entityId,
            int workshopId,
            int adminUserId,
            CancellationToken cancellationToken = default);

        Task<ServiceResult<bool>> ValidateAsync(
            string entityType,
            int entityId,
            string? lockToken,
            int workshopId,
            int userId,
            CancellationToken cancellationToken = default);

        Task<ServiceResult<bool>> ValidateServiceRequestItemAsync(
            int requestItemId,
            string? lockToken,
            int workshopId,
            int userId,
            CancellationToken cancellationToken = default);

        Task<ServiceResult<bool>> ValidateServiceOperationAsync(
            int operationId,
            string? lockToken,
            int workshopId,
            int userId,
            CancellationToken cancellationToken = default);

        Task<ServiceResult<bool>> ValidateServiceRecordImageAsync(
            int imageId,
            string? lockToken,
            int workshopId,
            int userId,
            CancellationToken cancellationToken = default);
    }
}
