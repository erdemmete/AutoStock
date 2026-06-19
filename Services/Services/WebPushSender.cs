using AutoStock.Repositories;
using AutoStock.Repositories.Entities;
using AutoStock.Services.Dtos.WebPush;
using AutoStock.Services.Interfaces;
using AutoStock.Services.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text.Json;
using WebPush;

namespace AutoStock.Services.Services
{
    public class WebPushSender : IWebPushSender
    {
        private readonly AppDbContext _context;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IOptions<WebPushSettings> _settings;
        private readonly ILogger<WebPushSender> _logger;

        public WebPushSender(
            AppDbContext context,
            IDateTimeProvider dateTimeProvider,
            IOptions<WebPushSettings> settings,
            ILogger<WebPushSender> logger)
        {
            _context = context;
            _dateTimeProvider = dateTimeProvider;
            _settings = settings;
            _logger = logger;
        }

        public async Task SendToUsersAsync(
            IReadOnlyCollection<int> userIds,
            Notification notification,
            WebPushPayloadDto payload,
            CancellationToken cancellationToken = default)
        {
            var settings = _settings.Value;

            if (!settings.Enabled)
            {
                _logger.LogInformation(
                    "Web Push skipped because it is disabled. NotificationId: {NotificationId}",
                    notification.Id);
                return;
            }

            if (string.IsNullOrWhiteSpace(settings.Subject) ||
                string.IsNullOrWhiteSpace(settings.PublicKey) ||
                string.IsNullOrWhiteSpace(settings.PrivateKey))
            {
                _logger.LogWarning(
                    "Web Push skipped because configuration is incomplete. NotificationId: {NotificationId}",
                    notification.Id);
                return;
            }

            var distinctUserIds = userIds
                .Where(x => x > 0)
                .Distinct()
                .ToList();

            if (!distinctUserIds.Any())
                return;

            var subscriptions = await _context.WebPushSubscriptions
                .Where(x => x.IsActive && distinctUserIds.Contains(x.UserId))
                .ToListAsync(cancellationToken);

            if (!subscriptions.Any())
                return;

            var vapidDetails = new VapidDetails(
                settings.Subject.Trim(),
                settings.PublicKey.Trim(),
                settings.PrivateKey.Trim());

            var client = new WebPushClient();
            var serializedPayload = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var now = _dateTimeProvider.Now;

            foreach (var subscription in subscriptions)
            {
                try
                {
                    var pushSubscription = new PushSubscription(
                        subscription.Endpoint,
                        subscription.P256dh,
                        subscription.Auth);

                    await client.SendNotificationAsync(
                        pushSubscription,
                        serializedPayload,
                        vapidDetails,
                        cancellationToken);

                    subscription.LastSuccessAt = now;
                    subscription.LastFailureAt = null;
                    subscription.UpdatedAt = now;
                }
                catch (WebPushException ex) when (
                    ex.StatusCode == HttpStatusCode.Gone ||
                    ex.StatusCode == HttpStatusCode.NotFound)
                {
                    subscription.IsActive = false;
                    subscription.LastFailureAt = now;
                    subscription.UpdatedAt = now;

                    _logger.LogInformation(
                        ex,
                        "Web Push subscription deactivated by push service. NotificationId: {NotificationId}, UserId: {UserId}, StatusCode: {StatusCode}",
                        notification.Id,
                        subscription.UserId,
                        ex.StatusCode);
                }
                catch (Exception ex)
                {
                    subscription.LastFailureAt = now;
                    subscription.UpdatedAt = now;

                    _logger.LogWarning(
                        ex,
                        "Web Push send failed. NotificationId: {NotificationId}, UserId: {UserId}",
                        notification.Id,
                        subscription.UserId);
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
