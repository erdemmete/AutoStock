using AutoStock.Repositories.Enums;
using AutoStock.Services.Dtos.Common;
using AutoStock.Services.Dtos.SecurityTokens;

namespace AutoStock.Services.Interfaces
{
    public interface IUserSecurityTokenService
    {
        Task<ServiceResult<UserSecurityTokenCreatedDto>> CreateAsync(CreateUserSecurityTokenRequestDto request);

        Task<ServiceResult<UserSecurityTokenValidationDto>> ValidateAsync(string token, UserSecurityTokenPurpose purpose);

        Task<ServiceResult<bool>> MarkAsUsedAsync(string token, UserSecurityTokenPurpose purpose, string? consumedIpAddress = null, string? consumedUserAgent = null);

        Task<ServiceResult<bool>> RevokeActiveTokensAsync(int userId, UserSecurityTokenPurpose purpose);

        Task<ServiceResult<UserSecurityTokenValidationDto>> ValidateByCodeAsync(string userName, string code, UserSecurityTokenPurpose purpose);

        Task<ServiceResult<bool>> MarkAsUsedByCodeAsync(string userName, string code, UserSecurityTokenPurpose purpose, string? consumedIpAddress = null, string? consumedUserAgent = null);
    }
}