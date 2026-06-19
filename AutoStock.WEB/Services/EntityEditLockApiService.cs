using AutoStock.Services.Dtos.EditLocks;
using AutoStock.WEB.Models.Common;

namespace AutoStock.WEB.Services
{
    public class EntityEditLockApiService : BaseApiService
    {
        public EntityEditLockApiService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor,
            ILogger<EntityEditLockApiService> logger)
            : base(httpClientFactory, configuration, httpContextAccessor, logger)
        {
        }

        public Task<ApiResponse<EntityEditLockDto>> AcquireAsync(EntityEditLockRequestDto request)
        {
            return PostJsonAsync<EntityEditLockRequestDto, EntityEditLockDto>(
                "/api/edit-locks/acquire",
                request,
                "Düzenleme kilidi alınamadı.");
        }

        public Task<ApiResponse<EntityEditLockDto>> GetStatusAsync(string entityType, int entityId)
        {
            var url = BuildUrlWithQuery("/api/edit-locks/status", new Dictionary<string, string?>
            {
                ["entityType"] = entityType,
                ["entityId"] = entityId.ToString()
            });

            return GetAsync<EntityEditLockDto>(url, "Düzenleme durumu alınamadı.");
        }

        public Task<ApiResponse<bool>> HeartbeatAsync(EntityEditLockRequestDto request)
        {
            return PostJsonAsync<EntityEditLockRequestDto, bool>(
                "/api/edit-locks/heartbeat",
                request,
                "Düzenleme kilidi yenilenemedi.");
        }

        public Task<ApiResponse<bool>> ReleaseAsync(EntityEditLockRequestDto request)
        {
            return PostJsonAsync<EntityEditLockRequestDto, bool>(
                "/api/edit-locks/release",
                request,
                "Düzenleme kilidi bırakılamadı.");
        }

        public Task<ApiResponse<bool>> ForceReleaseAsync(EntityEditLockRequestDto request)
        {
            return PostJsonAsync<EntityEditLockRequestDto, bool>(
                "/api/edit-locks/force-release",
                request,
                "Düzenleme kilidi kaldırılamadı.");
        }
    }
}
