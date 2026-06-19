using AutoStock.Services.Dtos.EditLocks;
using AutoStock.WEB.Services;
using Microsoft.AspNetCore.Mvc;

namespace AutoStock.WEB.Controllers
{
    public class EntityEditLocksController : BaseController
    {
        private readonly EntityEditLockApiService _entityEditLockApiService;

        public EntityEditLocksController(EntityEditLockApiService entityEditLockApiService)
        {
            _entityEditLockApiService = entityEditLockApiService;
        }

        [HttpPost("EntityEditLocks/Acquire")]
        public async Task<IActionResult> Acquire([FromBody] EntityEditLockRequestDto request)
        {
            var result = await _entityEditLockApiService.AcquireAsync(request);
            return StatusCode(result.StatusCode > 0 ? result.StatusCode : 200, result);
        }

        [HttpGet("EntityEditLocks/Status")]
        public async Task<IActionResult> Status([FromQuery] string entityType, [FromQuery] int entityId)
        {
            var result = await _entityEditLockApiService.GetStatusAsync(entityType, entityId);
            return StatusCode(result.StatusCode > 0 ? result.StatusCode : 200, result);
        }

        [HttpPost("EntityEditLocks/Heartbeat")]
        public async Task<IActionResult> Heartbeat([FromBody] EntityEditLockRequestDto request)
        {
            var result = await _entityEditLockApiService.HeartbeatAsync(request);
            return StatusCode(result.StatusCode > 0 ? result.StatusCode : 200, result);
        }

        [HttpPost("EntityEditLocks/Release")]
        public async Task<IActionResult> Release([FromBody] EntityEditLockRequestDto request)
        {
            var result = await _entityEditLockApiService.ReleaseAsync(request);
            return StatusCode(result.StatusCode > 0 ? result.StatusCode : 200, result);
        }

        [HttpPost("EntityEditLocks/ForceRelease")]
        public async Task<IActionResult> ForceRelease([FromBody] EntityEditLockRequestDto request)
        {
            var result = await _entityEditLockApiService.ForceReleaseAsync(request);
            return StatusCode(result.StatusCode > 0 ? result.StatusCode : 200, result);
        }
    }
}
