using AutoStock.Services.Dtos.Admin.Workshops;
using AutoStock.Services.Dtos.Common;

namespace AutoStock.Services.Interfaces
{
    public interface IAdminWorkshopService
    {
        Task<ServiceResult<List<AdminWorkshopListItemDto>>> GetListAsync();

        Task<ServiceResult<AdminWorkshopDetailDto>> GetByIdAsync(int id);

        Task<ServiceResult<int>> CreateAsync(CreateAdminWorkshopRequestDto request);
        Task<ServiceResult<bool>> UpdateSubscriptionAsync(int id, UpdateAdminWorkshopSubscriptionRequestDto request);
        Task<ServiceResult<List<AdminWorkshopUserDto>>> GetUsersAsync(int workshopId);

        Task<ServiceResult<int>> CreateUserAsync(int workshopId, CreateAdminWorkshopUserRequestDto request);

        Task<ServiceResult<bool>> UpdateUserStatusAsync(int workshopId, int userId, UpdateAdminWorkshopUserStatusRequestDto request);
        Task<ServiceResult<bool>> UpdateProfileAsync(int id, UpdateAdminWorkshopProfileRequestDto request);
    }
}