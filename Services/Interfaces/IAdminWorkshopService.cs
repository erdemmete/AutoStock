using AutoStock.Services.Dtos.Admin.Workshops;
using AutoStock.Services.Dtos.Common;

namespace AutoStock.Services.Interfaces
{
    public interface IAdminWorkshopService
    {
        Task<ServiceResult<List<AdminWorkshopListItemDto>>> GetListAsync();

        Task<ServiceResult<AdminWorkshopDetailDto>> GetByIdAsync(int id);

        Task<ServiceResult<AdminWorkshopUserCreatedDto>> CreateAsync(CreateAdminWorkshopRequestDto request);
        Task<ServiceResult<bool>> UpdateSubscriptionAsync(int id, UpdateAdminWorkshopSubscriptionRequestDto request);
        Task<ServiceResult<List<AdminWorkshopUserDto>>> GetUsersAsync(int workshopId);
        Task<ServiceResult<AdminWorkshopUserDetailDto>> GetUserDetailAsync(int workshopId, int userId);

        Task<ServiceResult<AdminWorkshopUserCreatedDto>> CreateUserAsync(int workshopId, CreateAdminWorkshopUserRequestDto request);
        Task<ServiceResult<bool>> UpdateUserAsync(int workshopId, int userId, UpdateAdminWorkshopUserRequestDto request);
        Task<ServiceResult<AdminWorkshopUserPasswordResetLinkDto>> CreateUserPasswordResetLinkAsync(int workshopId, int userId);
        Task<ServiceResult<bool>> UpdateUserStatusAsync(int workshopId, int userId, UpdateAdminWorkshopUserStatusRequestDto request);
        Task<ServiceResult<bool>> UpdateProfileAsync(int id, UpdateAdminWorkshopProfileRequestDto request);
        Task<ServiceResult<List<AdminWorkshopPartnerDto>>> GetPartnersAsync(int workshopId);

        Task<ServiceResult<int>> CreatePartnerAsync(int workshopId, CreateAdminWorkshopPartnerRequestDto request);

        Task<ServiceResult<bool>> UpdatePartnerAsync(int workshopId, int partnerId, UpdateAdminWorkshopPartnerRequestDto request);

        Task<ServiceResult<bool>> DeletePartnerAsync(int workshopId, int partnerId);

        Task<ServiceResult<SuggestedAdminWorkshopCredentialsDto>> SuggestUserCredentialsAsync(int workshopId, string fullName);
        Task<ServiceResult<PagedResult<AdminWorkshopListItemDto>>> GetPagedAsync(AdminWorkshopListQueryDto query);
        Task<ServiceResult<List<AdminWorkshopBankAccountDto>>> GetBankAccountsAsync(int workshopId);

        Task<ServiceResult<int>> CreateBankAccountAsync(
            int workshopId,
            CreateAdminWorkshopBankAccountRequestDto request);

        Task<ServiceResult<bool>> UpdateBankAccountAsync(
            int workshopId,
            int bankAccountId,
            UpdateAdminWorkshopBankAccountRequestDto request);

        Task<ServiceResult<bool>> DeleteBankAccountAsync(
            int workshopId,
            int bankAccountId);


    }
}
