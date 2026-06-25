using AutoStock.WEB.Models.Account;
using AutoStock.WEB.Models.Common;

namespace AutoStock.WEB.Services
{
    public class AccountApiService : BaseApiService
    {
        public AccountApiService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor,
            ILogger<AccountApiService> logger)
            : base(httpClientFactory, configuration, httpContextAccessor, logger)
        {
        }

        public Task<ApiResponse<AccountOverviewViewModel>> GetOverviewAsync()
        {
            return GetAsync<AccountOverviewViewModel>(
                "/api/Account/me",
                "Hesap bilgileri alınamadı.");
        }

        public Task<ApiResponse<bool>> UpdateEmailAsync(AccountEmailFormViewModel model)
        {
            return PutJsonAsync<AccountEmailFormViewModel, bool>(
                "/api/Account/email",
                model,
                "E-posta bilgisi güncellenemedi.");
        }

        public Task<ApiResponse<bool>> SendEmailConfirmationAsync(string confirmationUrlBase)
        {
            return PostJsonAsync<object, bool>(
                "/api/Account/email-confirmation/request",
                new { confirmationUrlBase },
                "Doğrulama e-postası gönderilemedi.");
        }

        public Task<ApiResponse<bool>> ConfirmEmailAsync(int userId, string token)
        {
            return PostJsonAsync<object, bool>(
                "/api/Account/email-confirmation/confirm",
                new { userId, token },
                "E-posta adresi doğrulanamadı.");
        }

        public Task<ApiResponse<bool>> UpdatePhoneAsync(AccountPhoneFormViewModel model)
        {
            return PutJsonAsync<AccountPhoneFormViewModel, bool>(
                "/api/Account/phone",
                model,
                "Telefon numarası güncellenemedi.");
        }

        public Task<ApiResponse<bool>> ChangePasswordAsync(ChangePasswordFormViewModel model)
        {
            return PostJsonAsync<ChangePasswordFormViewModel, bool>(
                "/api/Account/password/change",
                model,
                "Şifre değiştirilemedi.");
        }

        public Task<ApiResponse<WorkshopProfileFormViewModel>> GetWorkshopProfileAsync()
        {
            return GetAsync<WorkshopProfileFormViewModel>(
                "/api/Account/workshop",
                "Servis bilgileri alınamadı.");
        }

        public Task<ApiResponse<bool>> UpdateWorkshopProfileAsync(WorkshopProfileFormViewModel model)
        {
            return PutJsonAsync<WorkshopProfileFormViewModel, bool>(
                "/api/Account/workshop",
                model,
                "Servis bilgileri güncellenemedi.");
        }

        public Task<ApiResponse<int>> CreateWorkshopBankAccountAsync(CreateAccountWorkshopBankAccountViewModel model)
        {
            return PostJsonAsync<CreateAccountWorkshopBankAccountViewModel, int>(
                "/api/Account/workshop/bank-accounts",
                model,
                "Banka hesabı eklenemedi.");
        }

        public Task<ApiResponse<bool>> UpdateWorkshopBankAccountAsync(UpdateAccountWorkshopBankAccountViewModel model)
        {
            return PutJsonAsync<UpdateAccountWorkshopBankAccountViewModel, bool>(
                $"/api/Account/workshop/bank-accounts/{model.BankAccountId}",
                model,
                "Banka hesabı güncellenemedi.");
        }

        public Task<ApiResponse<bool>> DeleteWorkshopBankAccountAsync(int bankAccountId)
        {
            return DeleteAsync<bool>(
                $"/api/Account/workshop/bank-accounts/{bankAccountId}",
                "Banka hesabı silinemedi.");
        }

        public Task<ApiResponse<MembershipInfoViewModel>> GetMembershipAsync()
        {
            return GetAsync<MembershipInfoViewModel>(
                "/api/Account/membership",
                "Üyelik bilgileri alınamadı.");
        }
    }
}
