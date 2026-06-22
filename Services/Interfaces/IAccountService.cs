using AutoStock.Services.Dtos.Account;
using AutoStock.Services.Dtos.Common;

namespace AutoStock.Services.Interfaces
{
    public interface IAccountService
    {
        Task<ServiceResult<AccountOverviewDto>> GetOverviewAsync(int userId);

        Task<ServiceResult<bool>> UpdateEmailAsync(int userId, UpdateAccountEmailRequestDto request);

        Task<ServiceResult<bool>> UpdatePhoneAsync(int userId, UpdateAccountPhoneRequestDto request);

        Task<ServiceResult<bool>> ChangePasswordAsync(int userId, ChangePasswordRequestDto request);

        Task<ServiceResult<WorkshopProfileManagementDto>> GetWorkshopProfileAsync(int userId, int workshopId);

        Task<ServiceResult<bool>> UpdateWorkshopProfileAsync(
            int userId,
            int workshopId,
            UpdateWorkshopProfileManagementRequestDto request);

        Task<ServiceResult<int>> CreateWorkshopBankAccountAsync(
            int userId,
            int workshopId,
            CreateAccountWorkshopBankAccountRequestDto request);

        Task<ServiceResult<bool>> UpdateWorkshopBankAccountAsync(
            int userId,
            int workshopId,
            int bankAccountId,
            UpdateAccountWorkshopBankAccountRequestDto request);

        Task<ServiceResult<bool>> DeleteWorkshopBankAccountAsync(
            int userId,
            int workshopId,
            int bankAccountId);

        Task<ServiceResult<MembershipInfoDto>> GetMembershipAsync(int userId, int workshopId);

        Task<ServiceResult<bool>> StartForgotPasswordAsync(ForgotPasswordRequestDto request);
    }
}
