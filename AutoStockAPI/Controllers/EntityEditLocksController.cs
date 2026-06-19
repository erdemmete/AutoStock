using AutoStock.Services.Constants;
using AutoStock.Services.Dtos.EditLocks;
using AutoStock.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoStock.API.Controllers
{
    [Authorize(Roles = AppRoles.OwnerOrStaff)]
    [ApiController]
    [Route("api/edit-locks")]
    public class EntityEditLocksController : BaseApiController
    {
        private readonly IEntityEditLockService _entityEditLockService;

        public EntityEditLocksController(IEntityEditLockService entityEditLockService)
        {
            _entityEditLockService = entityEditLockService;
        }

        [HttpPost("acquire")]
        public async Task<IActionResult> Acquire(EntityEditLockRequestDto request)
        {
            var context = GetContext();
            if (context.Result is not null)
                return context.Result;

            var result = await _entityEditLockService.AcquireAsync(
                request.EntityType,
                request.EntityId,
                request.LockToken,
                context.WorkshopId,
                context.UserId);

            return ToActionResult(result);
        }

        [HttpGet("status")]
        public async Task<IActionResult> Status([FromQuery] string entityType, [FromQuery] int entityId)
        {
            var context = GetContext();
            if (context.Result is not null)
                return context.Result;

            var result = await _entityEditLockService.GetStatusAsync(
                entityType,
                entityId,
                context.WorkshopId,
                context.UserId);

            return ToActionResult(result);
        }

        [HttpPost("heartbeat")]
        public async Task<IActionResult> Heartbeat(EntityEditLockRequestDto request)
        {
            var context = GetContext();
            if (context.Result is not null)
                return context.Result;

            var result = await _entityEditLockService.HeartbeatAsync(
                request.EntityType,
                request.EntityId,
                request.LockToken,
                context.WorkshopId,
                context.UserId);

            return ToActionResult(result);
        }

        [HttpPost("release")]
        public async Task<IActionResult> Release(EntityEditLockRequestDto request)
        {
            var context = GetContext();
            if (context.Result is not null)
                return context.Result;

            var result = await _entityEditLockService.ReleaseAsync(
                request.EntityType,
                request.EntityId,
                request.LockToken,
                context.WorkshopId,
                context.UserId);

            return ToActionResult(result);
        }

        [HttpPost("force-release")]
        [Authorize(Roles = AppRoles.Owner + "," + AppRoles.Admin)]
        public async Task<IActionResult> ForceRelease(EntityEditLockRequestDto request)
        {
            var context = GetContext();
            if (context.Result is not null)
                return context.Result;

            var result = await _entityEditLockService.ForceReleaseAsync(
                request.EntityType,
                request.EntityId,
                context.WorkshopId,
                context.UserId,
                GetCurrentUserRole());

            return ToActionResult(result);
        }

        private (int WorkshopId, int UserId, IActionResult? Result) GetContext()
        {
            var workshopIdResult = GetCurrentWorkshopId();
            if (workshopIdResult.IsFailure)
                return (0, 0, UnauthorizedResult(workshopIdResult));

            var userIdResult = GetCurrentUserId();
            if (userIdResult.IsFailure)
                return (0, 0, UnauthorizedResult(userIdResult));

            return (workshopIdResult.Data, userIdResult.Data, null);
        }
    }
}
