using AutoStock.Repositories;
using AutoStock.Repositories.Entities;
using AutoStock.Services.Dtos.Common;
using AutoStock.Services.Dtos.WebPush;
using AutoStock.Services.Interfaces;
using AutoStock.Services.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AutoStock.Services.Services
{
    public class WebPushSubscriptionService : IWebPushSubscriptionService
    {
        private readonly AppDbContext _context;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IOptions<WebPushSettings> _settings;
        private readonly ILogger<WebPushSubscriptionService> _logger;

        public WebPushSubscriptionService(
            AppDbContext context,
            IDateTimeProvider dateTimeProvider,
            IOptions<WebPushSettings> settings,
            ILogger<WebPushSubscriptionService> logger)
        {
            _context = context;
            _dateTimeProvider = dateTimeProvider;
            _settings = settings;
            _logger = logger;
        }

        public async Task<ServiceResult<WebPushSubscriptionStatusDto>> GetStatusAsync(
            int userId,
            int? workshopId,
            string? endpoint)
        {
            var publicKeyResult = await GetPublicKeyAsync();
            var hasActiveSubscription = false;

            if (!string.IsNullOrWhiteSpace(endpoint))
            {
                hasActiveSubscription = await _context.WebPushSubscriptions
                    .AsNoTracking()
                    .AnyAsync(x =>
                        x.UserId == userId &&
                        x.Endpoint == endpoint &&
                        x.IsActive &&
                        x.WorkshopId == workshopId);
            }

            return ServiceResult<WebPushSubscriptionStatusDto>.Success(new WebPushSubscriptionStatusDto
            {
                IsEnabled = publicKeyResult.IsSuccess,
                PublicKey = publicKeyResult.IsSuccess ? publicKeyResult.Data : null,
                HasActiveSubscription = hasActiveSubscription
            });
        }

        public Task<ServiceResult<string>> GetPublicKeyAsync()
        {
            var settings = _settings.Value;

            if (!settings.Enabled)
            {
                _logger.LogInformation("Web Push is disabled by configuration.");
                return Task.FromResult(ServiceResult<string>.Fail("Tarayıcı bildirimleri yapılandırılmamış."));
            }

            if (string.IsNullOrWhiteSpace(settings.Subject) ||
                string.IsNullOrWhiteSpace(settings.PublicKey) ||
                string.IsNullOrWhiteSpace(settings.PrivateKey))
            {
                _logger.LogWarning("Web Push configuration is incomplete.");
                return Task.FromResult(ServiceResult<string>.Fail("Tarayıcı bildirimleri yapılandırılmamış."));
            }

            return Task.FromResult(ServiceResult<string>.Success(settings.PublicKey.Trim()));
        }

        public async Task<ServiceResult<bool>> UpsertAsync(
            int userId,
            int? workshopId,
            WebPushSubscriptionRequestDto request)
        {
            var configResult = await GetPublicKeyAsync();
            if (configResult.IsFailure)
                return ServiceResult<bool>.Fail(configResult.ErrorMessage);

            if (request is null ||
                string.IsNullOrWhiteSpace(request.Endpoint) ||
                string.IsNullOrWhiteSpace(request.Keys?.P256dh) ||
                string.IsNullOrWhiteSpace(request.Keys.Auth))
            {
                return ServiceResult<bool>.Fail("Bildirim aboneliği bilgisi alınamadı.");
            }

            var now = _dateTimeProvider.Now;
            var endpoint = request.Endpoint.Trim();

            var existing = await _context.WebPushSubscriptions
                .FirstOrDefaultAsync(x => x.Endpoint == endpoint);

            if (existing is null)
            {
                existing = new WebPushSubscription
                {
                    Endpoint = endpoint,
                    CreatedAt = now
                };

                _context.WebPushSubscriptions.Add(existing);
            }

            existing.UserId = userId;
            existing.WorkshopId = workshopId;
            existing.P256dh = request.Keys.P256dh.Trim();
            existing.Auth = request.Keys.Auth.Trim();
            existing.UserAgent = NormalizeNullable(request.UserAgent, 500);
            existing.IsActive = true;
            existing.UpdatedAt = now;
            existing.LastFailureAt = null;

            await _context.SaveChangesAsync();

            return ServiceResult<bool>.Success(true);
        }

        public async Task<ServiceResult<bool>> DisableAsync(int userId, string endpoint)
        {
            if (string.IsNullOrWhiteSpace(endpoint))
                return ServiceResult<bool>.Success(true);

            var subscription = await _context.WebPushSubscriptions
                .FirstOrDefaultAsync(x =>
                    x.UserId == userId &&
                    x.Endpoint == endpoint.Trim());

            if (subscription is null)
                return ServiceResult<bool>.Success(true);

            subscription.IsActive = false;
            subscription.UpdatedAt = _dateTimeProvider.Now;

            await _context.SaveChangesAsync();

            return ServiceResult<bool>.Success(true);
        }

        public async Task<ServiceResult<bool>> DisableAllForUserAsync(int userId)
        {
            var subscriptions = await _context.WebPushSubscriptions
                .Where(x => x.UserId == userId && x.IsActive)
                .ToListAsync();

            if (!subscriptions.Any())
                return ServiceResult<bool>.Success(true);

            var now = _dateTimeProvider.Now;

            foreach (var subscription in subscriptions)
            {
                subscription.IsActive = false;
                subscription.UpdatedAt = now;
            }

            await _context.SaveChangesAsync();

            return ServiceResult<bool>.Success(true);
        }

        private static string? NormalizeNullable(string? value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var normalized = value.Trim();
            return normalized.Length <= maxLength ? normalized : normalized[..maxLength];
        }
    }
}
