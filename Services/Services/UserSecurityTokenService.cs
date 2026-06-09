using AutoStock.Repositories;
using AutoStock.Repositories.Entities;
using AutoStock.Repositories.Enums;
using AutoStock.Services.Dtos.Common;
using AutoStock.Services.Dtos.SecurityTokens;
using AutoStock.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace AutoStock.Services.Services
{
    public class UserSecurityTokenService : IUserSecurityTokenService
    {
        private static readonly TimeSpan DefaultTokenLifetime = TimeSpan.FromHours(24);

        private readonly AppDbContext _context;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IAuditContextAccessor _auditContextAccessor;

        public UserSecurityTokenService(
            AppDbContext context,
            IDateTimeProvider dateTimeProvider,
            IAuditContextAccessor auditContextAccessor)
        {
            _context = context;
            _dateTimeProvider = dateTimeProvider;
            _auditContextAccessor = auditContextAccessor;
        }

        public async Task<ServiceResult<UserSecurityTokenCreatedDto>> CreateAsync(CreateUserSecurityTokenRequestDto request)
        {
            if (request.UserId <= 0)
                return ServiceResult<UserSecurityTokenCreatedDto>.Fail("Geçerli bir kullanıcı seçilmelidir.");

            var userExists = await _context.Users
                .AnyAsync(x => x.Id == request.UserId);

            if (!userExists)
            {
                return ServiceResult<UserSecurityTokenCreatedDto>.Fail(
                    "Kullanıcı bulunamadı.",
                    HttpStatusCode.NotFound);
            }

            var now = _dateTimeProvider.Now;

            await RevokeActiveTokensInternalAsync(
                request.UserId,
                request.Purpose,
                now);

            var rawToken = GenerateRawToken();
            var tokenHash = HashToken(rawToken);

            var rawCode = await GenerateUniqueInviteCodeAsync(now);
            var codeHash = HashInviteCode(rawCode);

            var token = new UserSecurityToken
            {
                UserId = request.UserId,
                TokenHash = tokenHash,
                CodeHash = codeHash,
                Purpose = request.Purpose,
                DeliveryChannel = request.DeliveryChannel,
                ExpiresAt = now.Add(request.ValidFor ?? DefaultTokenLifetime),
                CreatedAt = now,
                CreatedByUserId = request.CreatedByUserId ?? _auditContextAccessor.Current.UserId
            };

            _context.UserSecurityTokens.Add(token);

            await _context.SaveChangesAsync();

            var response = new UserSecurityTokenCreatedDto
            {
                UserId = request.UserId,
                Token = rawToken,
                Code = rawCode,
                Purpose = token.Purpose,
                DeliveryChannel = token.DeliveryChannel,
                ExpiresAt = token.ExpiresAt
            };

            return ServiceResult<UserSecurityTokenCreatedDto>.Success(response);
        }

        public async Task<ServiceResult<UserSecurityTokenValidationDto>> ValidateAsync(string token, UserSecurityTokenPurpose purpose)
        {
            if (string.IsNullOrWhiteSpace(token))
                return ServiceResult<UserSecurityTokenValidationDto>.Fail("Bağlantı geçersiz.");

            var tokenHash = HashToken(token.Trim());
            var now = _dateTimeProvider.Now;

            var securityToken = await _context.UserSecurityTokens
                .AsNoTracking()
                .Include(x => x.User)
                .FirstOrDefaultAsync(x =>
                    x.TokenHash == tokenHash &&
                    x.Purpose == purpose);

            if (securityToken == null)
                return ServiceResult<UserSecurityTokenValidationDto>.Fail("Bağlantı geçersiz.");

            if (securityToken.UsedAt.HasValue)
                return ServiceResult<UserSecurityTokenValidationDto>.Fail("Bu bağlantı daha önce kullanılmış.");

            if (securityToken.RevokedAt.HasValue)
                return ServiceResult<UserSecurityTokenValidationDto>.Fail("Bu bağlantı artık geçerli değil.");

            if (securityToken.ExpiresAt <= now)
                return ServiceResult<UserSecurityTokenValidationDto>.Fail("Bu bağlantının süresi dolmuş.");

            if (!securityToken.User.IsActive)
                return ServiceResult<UserSecurityTokenValidationDto>.Fail("Kullanıcı pasif durumda.");

            var response = new UserSecurityTokenValidationDto
            {
                TokenId = securityToken.Id,
                UserId = securityToken.UserId,
                FullName = securityToken.User.FullName,
                UserName = securityToken.User.UserName!,
                Email = securityToken.User.Email,
                PhoneNumber = securityToken.User.PhoneNumber,
                Purpose = securityToken.Purpose,
                ExpiresAt = securityToken.ExpiresAt
            };

            return ServiceResult<UserSecurityTokenValidationDto>.Success(response);
        }

        public async Task<ServiceResult<bool>> MarkAsUsedAsync(string token, UserSecurityTokenPurpose purpose, string? consumedIpAddress = null, string? consumedUserAgent = null)
        {
            if (string.IsNullOrWhiteSpace(token))
                return ServiceResult<bool>.Fail("Bağlantı geçersiz.");

            var tokenHash = HashToken(token.Trim());
            var now = _dateTimeProvider.Now;

            var securityToken = await _context.UserSecurityTokens
                .Include(x => x.User)
                .FirstOrDefaultAsync(x =>
                    x.TokenHash == tokenHash &&
                    x.Purpose == purpose);

            if (securityToken == null)
                return ServiceResult<bool>.Fail("Bağlantı geçersiz.");

            if (securityToken.UsedAt.HasValue)
                return ServiceResult<bool>.Fail("Bu bağlantı daha önce kullanılmış.");

            if (securityToken.RevokedAt.HasValue)
                return ServiceResult<bool>.Fail("Bu bağlantı artık geçerli değil.");

            if (securityToken.ExpiresAt <= now)
                return ServiceResult<bool>.Fail("Bu bağlantının süresi dolmuş.");

            if (!securityToken.User.IsActive)
                return ServiceResult<bool>.Fail("Kullanıcı pasif durumda.");

            securityToken.UsedAt = now;
            securityToken.ConsumedIpAddress = Limit(consumedIpAddress, 64);
            securityToken.ConsumedUserAgent = Limit(consumedUserAgent, 512);

            await _context.SaveChangesAsync();

            return ServiceResult<bool>.Success(true);
        }

        public async Task<ServiceResult<bool>> MarkAsUsedByCodeAsync(string userName, string code, UserSecurityTokenPurpose purpose, string? consumedIpAddress = null, string? consumedUserAgent = null)
        {
            if (string.IsNullOrWhiteSpace(userName))
                return ServiceResult<bool>.Fail("Kullanıcı adı zorunludur.");

            if (string.IsNullOrWhiteSpace(code))
                return ServiceResult<bool>.Fail("Davet kodu zorunludur.");

            var normalizedUserName = userName.Trim().ToUpperInvariant();
            var codeHash = HashInviteCode(code);
            var now = _dateTimeProvider.Now;

            var securityToken = await _context.UserSecurityTokens
                .Include(x => x.User)
                .FirstOrDefaultAsync(x =>
                    x.User.NormalizedUserName == normalizedUserName &&
                    x.CodeHash == codeHash &&
                    x.Purpose == purpose);

            if (securityToken == null)
                return ServiceResult<bool>.Fail("Davet kodu geçersiz.");

            if (securityToken.UsedAt.HasValue)
                return ServiceResult<bool>.Fail("Bu davet kodu daha önce kullanılmış.");

            if (securityToken.RevokedAt.HasValue)
                return ServiceResult<bool>.Fail("Bu davet kodu artık geçerli değil.");

            if (securityToken.ExpiresAt <= now)
                return ServiceResult<bool>.Fail("Bu davet kodunun süresi dolmuş.");

            if (!securityToken.User.IsActive)
                return ServiceResult<bool>.Fail("Kullanıcı pasif durumda.");

            securityToken.UsedAt = now;
            securityToken.ConsumedIpAddress = Limit(consumedIpAddress, 64);
            securityToken.ConsumedUserAgent = Limit(consumedUserAgent, 512);

            await _context.SaveChangesAsync();

            return ServiceResult<bool>.Success(true);
        }

        public async Task<ServiceResult<bool>> RevokeActiveTokensAsync(int userId, UserSecurityTokenPurpose purpose)
        {
            if (userId <= 0)
                return ServiceResult<bool>.Fail("Geçerli bir kullanıcı seçilmelidir.");

            var now = _dateTimeProvider.Now;

            await RevokeActiveTokensInternalAsync(userId, purpose, now);

            await _context.SaveChangesAsync();

            return ServiceResult<bool>.Success(true);
        }

        public async Task<ServiceResult<UserSecurityTokenValidationDto>> ValidateByCodeAsync(string userName, string code, UserSecurityTokenPurpose purpose)
        {
            if (string.IsNullOrWhiteSpace(userName))
                return ServiceResult<UserSecurityTokenValidationDto>.Fail("Kullanıcı adı zorunludur.");

            if (string.IsNullOrWhiteSpace(code))
                return ServiceResult<UserSecurityTokenValidationDto>.Fail("Davet kodu zorunludur.");

            var normalizedUserName = userName.Trim().ToUpperInvariant();
            var codeHash = HashInviteCode(code);
            var now = _dateTimeProvider.Now;

            var securityToken = await _context.UserSecurityTokens
                .AsNoTracking()
                .Include(x => x.User)
                .FirstOrDefaultAsync(x =>
                    x.User.NormalizedUserName == normalizedUserName &&
                    x.CodeHash == codeHash &&
                    x.Purpose == purpose);

            return ValidateCodeSecurityToken(securityToken, now);
        }

       

        private async Task RevokeActiveTokensInternalAsync(int userId, UserSecurityTokenPurpose purpose, DateTime now)
        {
            var activeTokens = await _context.UserSecurityTokens
                .Where(x =>
                    x.UserId == userId &&
                    x.Purpose == purpose &&
                    x.UsedAt == null &&
                    x.RevokedAt == null &&
                    x.ExpiresAt > now)
                .ToListAsync();

            foreach (var activeToken in activeTokens)
            {
                activeToken.RevokedAt = now;
            }
        }

        private static string GenerateRawToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);

            return Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        private ServiceResult<UserSecurityTokenValidationDto> ValidateCodeSecurityToken(UserSecurityToken? securityToken, DateTime now)
        {
            if (securityToken == null)
                return ServiceResult<UserSecurityTokenValidationDto>.Fail("Davet kodu geçersiz.");

            if (securityToken.UsedAt.HasValue)
                return ServiceResult<UserSecurityTokenValidationDto>.Fail("Bu davet kodu daha önce kullanılmış.");

            if (securityToken.RevokedAt.HasValue)
                return ServiceResult<UserSecurityTokenValidationDto>.Fail("Bu davet kodu artık geçerli değil.");

            if (securityToken.ExpiresAt <= now)
                return ServiceResult<UserSecurityTokenValidationDto>.Fail("Bu davet kodunun süresi dolmuş.");

            if (!securityToken.User.IsActive)
                return ServiceResult<UserSecurityTokenValidationDto>.Fail("Kullanıcı pasif durumda.");

            var response = new UserSecurityTokenValidationDto
            {
                TokenId = securityToken.Id,
                UserId = securityToken.UserId,
                FullName = securityToken.User.FullName,
                UserName = securityToken.User.UserName!,
                Email = securityToken.User.Email,
                PhoneNumber = securityToken.User.PhoneNumber,
                Purpose = securityToken.Purpose,
                ExpiresAt = securityToken.ExpiresAt
            };

            return ServiceResult<UserSecurityTokenValidationDto>.Success(response);
        }

        private static string HashToken(string token)
        {
            var bytes = Encoding.UTF8.GetBytes(token);
            var hashBytes = SHA256.HashData(bytes);

            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }

        private static string? Limit(string? value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var safeValue = value.Trim();

            return safeValue.Length <= maxLength
                ? safeValue
                : safeValue[..maxLength];
        }
       
        private async Task<string> GenerateUniqueInviteCodeAsync(DateTime now)
        {
            for (var i = 0; i < 10; i++)
            {
                var code = GenerateInviteCode();
                var codeHash = HashInviteCode(code);

                var exists = await _context.UserSecurityTokens
                    .AnyAsync(x =>
                        x.CodeHash == codeHash &&
                        x.UsedAt == null &&
                        x.RevokedAt == null &&
                        x.ExpiresAt > now);

                if (!exists)
                    return code;
            }

            throw new InvalidOperationException("Davet kodu oluşturulamadı.");
        }

        private static string GenerateInviteCode()
        {
            const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

            var bytes = RandomNumberGenerator.GetBytes(8);

            var chars = bytes
                .Select(b => alphabet[b % alphabet.Length])
                .ToArray();

            var raw = new string(chars);

            return $"{raw[..4]}-{raw[4..]}";
        }

        private static string NormalizeCode(string code)
        {
            var chars = code
                .Where(char.IsLetterOrDigit)
                .Select(char.ToUpperInvariant)
                .ToArray();

            return new string(chars);
        }

        private static string HashInviteCode(string code)
        {
            return HashToken(NormalizeCode(code));
        }
    }
}