using AutoStock.Services.Dtos.Common;
using AutoStock.Services.Dtos.SupportRequests;

namespace AutoStock.Services.Interfaces
{
    public interface ISupportRequestService
    {
        Task<ServiceResult<PagedResult<SupportRequestListItemDto>>> GetPagedForWorkshopAsync(
    SupportRequestListQueryDto query,
    int workshopId,
    int currentUserId,
    string? currentUserRole);

        Task<ServiceResult<SupportRequestDetailDto>> GetByIdForWorkshopAsync(
            int id,
            int workshopId,
            int currentUserId,
            string? currentUserRole);
        Task<ServiceResult<int>> CreateIssueAsync(
            CreateIssueSupportRequestDto request,
            int workshopId,
            int createdByUserId);

        Task<ServiceResult<int>> CreateUserCreateRequestAsync(
            CreateUserSupportRequestDto request,
            int workshopId,
            int createdByUserId,
            string? currentUserRole);

        Task<ServiceResult<int>> CancelForWorkshopAsync(
            int id,
            int workshopId,
            int currentUserId);

        Task<ServiceResult<PagedResult<SupportRequestListItemDto>>> GetPagedForAdminAsync(
            AdminSupportRequestListQueryDto query);

        Task<ServiceResult<SupportRequestDetailDto>> GetByIdForAdminAsync(
            int id);

        Task<ServiceResult<int>> AnswerAsync(
            AdminAnswerSupportRequestDto request,
            int respondedByUserId);

        Task<ServiceResult<int>> UpdateStatusAsync(
            AdminUpdateSupportRequestStatusDto request,
            int updatedByUserId);
    }
}