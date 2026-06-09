using AutoStock.Services.Dtos.Auth;
using AutoStock.Services.Dtos.Common;

namespace AutoStock.Services.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request);

        Task<AuthResponseDto> LoginAsync(LoginRequestDto request);

        Task<ServiceResult<PasswordActionTokenInfoDto>> ValidatePasswordSetupTokenAsync(
            ValidatePasswordActionTokenRequestDto request);

        Task<ServiceResult<AuthResponseDto>> CompletePasswordSetupAsync(
            CompletePasswordActionRequestDto request,
            string? ipAddress,
            string? userAgent);

        Task<ServiceResult<PasswordActionTokenInfoDto>> ValidatePasswordResetTokenAsync(
            ValidatePasswordActionTokenRequestDto request);

        Task<ServiceResult<AuthResponseDto>> CompletePasswordResetAsync(
            CompletePasswordActionRequestDto request,
            string? ipAddress,
            string? userAgent);
        Task<ServiceResult<PasswordActionTokenInfoDto>> ValidatePasswordSetupCodeAsync(
    ValidatePasswordActionCodeRequestDto request);

        Task<ServiceResult<AuthResponseDto>> CompletePasswordSetupByCodeAsync(
            CompletePasswordActionCodeRequestDto request,
            string? ipAddress,
            string? userAgent);
    }
}