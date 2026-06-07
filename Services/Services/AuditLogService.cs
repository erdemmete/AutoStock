using AutoStock.Repositories;
using AutoStock.Repositories.Entities;
using AutoStock.Services.Dtos.AuditLogs;
using AutoStock.Services.Interfaces;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AutoStock.Services.Services
{
    public class AuditLogService(
        IAuditLogRepository auditLogRepository,
        IUnitOfWork unitOfWork,
        IAuditContextAccessor auditContextAccessor) : IAuditLogService
    {
        private const int MaxDescriptionLength = 1000;
        private const int MaxJsonLength = 4000;
        private const int MaxUserFullNameLength = 150;
        private const int MaxUserRoleLength = 50;
        private const int MaxIpAddressLength = 64;
        private const int MaxUserAgentLength = 500;

        public async Task AddAsync(
            AuditLogCreateDto request,
            CancellationToken cancellationToken = default)
        {
            var context = auditContextAccessor.Current;

            var auditLog = new AuditLog
            {
                WorkshopId = request.WorkshopId ?? context.WorkshopId,
                UserId = request.UserId ?? context.UserId,
                UserFullName = Limit(request.UserFullName ?? context.UserFullName, MaxUserFullNameLength),
                UserRole = Limit(request.UserRole ?? context.UserRole, MaxUserRoleLength),

                ActionType = request.ActionType,
                EntityType = request.EntityType,
                EntityId = request.EntityId,

                Description = LimitRequired(request.Description, MaxDescriptionLength),

                OldValuesJson = ToSafeJson(request.OldValues),
                NewValuesJson = ToSafeJson(request.NewValues),

                IpAddress = Limit(request.IpAddress ?? context.IpAddress, MaxIpAddressLength),
                UserAgent = Limit(request.UserAgent ?? context.UserAgent, MaxUserAgentLength),

                CreatedAt = DateTime.UtcNow
            };

            await auditLogRepository.AddAsync(auditLog, cancellationToken);
        }

        public async Task WriteAsync(
            AuditLogCreateDto request,
            CancellationToken cancellationToken = default)
        {
            await AddAsync(request, cancellationToken);
            await unitOfWork.SaveChangesAsync();
        }

        private static string LimitRequired(string? value, int maxLength)
        {
            var safeValue = string.IsNullOrWhiteSpace(value)
                ? "Audit log kaydı oluşturuldu."
                : value.Trim();

            return safeValue.Length <= maxLength
                ? safeValue
                : safeValue[..maxLength];
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

        private static string? ToSafeJson(object? value)
        {
            if (value is null)
                return null;

            try
            {
                var json = value is string stringValue
                    ? stringValue
                    : JsonSerializer.Serialize(value, new JsonSerializerOptions
                    {
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                    });

                if (string.IsNullOrWhiteSpace(json))
                    return null;

                if (json.Length <= MaxJsonLength)
                    return json;

                return JsonSerializer.Serialize(new
                {
                    truncated = true,
                    originalLength = json.Length,
                    preview = json[..Math.Min(1000, json.Length)]
                });
            }
            catch
            {
                return JsonSerializer.Serialize(new
                {
                    serializationFailed = true
                });
            }
        }
    }
}