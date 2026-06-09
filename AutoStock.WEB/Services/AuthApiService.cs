using AutoStock.WEB.Models;
using AutoStock.WEB.Models.Auth;
using AutoStock.WEB.Models.Common;

namespace AutoStock.WEB.Services
{
    public class AuthApiService : BaseApiService
    {
        public AuthApiService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor,
            ILogger<AuthApiService> logger)
            : base(httpClientFactory, configuration, httpContextAccessor, logger)
        {
        }

        public async Task<ApiResponse<PasswordActionTokenInfoViewModel>> ValidatePasswordSetupTokenAsync(
            string token)
        {
            var requestBody = new
            {
                token
            };

            return await PostJsonAsync<object, PasswordActionTokenInfoViewModel>(
                "/api/Auth/password-setup/validate",
                requestBody,
                "Kurulum bağlantısı doğrulanırken hata oluştu.");
        }

        public async Task<ApiResponse<AuthResponseViewModel>> CompletePasswordSetupAsync(
            PasswordSetupViewModel model)
        {
            var requestBody = new
            {
                token = model.Token,
                newPassword = model.NewPassword,
                confirmNewPassword = model.ConfirmNewPassword
            };

            return await PostJsonAsync<object, AuthResponseViewModel>(
                "/api/Auth/password-setup/complete",
                requestBody,
                "Şifre oluşturulurken hata oluştu.");
        }

        public async Task<ApiResponse<PasswordActionTokenInfoViewModel>> ValidatePasswordSetupCodeAsync(
            string userName,
            string code)
        {
            var requestBody = new
            {
                userName,
                code
            };

            return await PostJsonAsync<object, PasswordActionTokenInfoViewModel>(
                "/api/Auth/password-setup/code/validate",
                requestBody,
                "Davet kodu doğrulanırken hata oluştu.");
        }

        public async Task<ApiResponse<AuthResponseViewModel>> CompletePasswordSetupByCodeAsync(
            InviteCodeViewModel model)
        {
            var requestBody = new
            {
                userName = model.UserName,
                code = model.Code,
                newPassword = model.NewPassword,
                confirmNewPassword = model.ConfirmNewPassword
            };

            return await PostJsonAsync<object, AuthResponseViewModel>(
                "/api/Auth/password-setup/code/complete",
                requestBody,
                "Davet kodu ile şifre oluşturulurken hata oluştu.");
        }

        public async Task<ApiResponse<PasswordActionTokenInfoViewModel>> ValidatePasswordResetTokenAsync(
    string token)
        {
            var requestBody = new
            {
                token
            };

            return await PostJsonAsync<object, PasswordActionTokenInfoViewModel>(
                "/api/Auth/password-reset/validate",
                requestBody,
                "Şifre sıfırlama bağlantısı doğrulanırken hata oluştu.");
        }

        public async Task<ApiResponse<AuthResponseViewModel>> CompletePasswordResetAsync(
            PasswordResetViewModel model)
        {
            var requestBody = new
            {
                token = model.Token,
                newPassword = model.NewPassword,
                confirmNewPassword = model.ConfirmNewPassword
            };

            return await PostJsonAsync<object, AuthResponseViewModel>(
                "/api/Auth/password-reset/complete",
                requestBody,
                "Şifre sıfırlanırken hata oluştu.");
        }
    }
}