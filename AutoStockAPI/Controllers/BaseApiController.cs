using AutoStock.Services.Dtos.Common;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;

namespace AutoStock.API.Controllers
{
    public abstract class BaseApiController : ControllerBase
    {
        protected const string EditLockTokenHeaderName = "X-Sente-Edit-Lock-Token";
        protected const string ServiceRecordIdHeaderName = "X-Sente-ServiceRecord-Id";

        protected ServiceResult<int> GetCurrentWorkshopId()
        {
            var workshopIdClaim =
                User.FindFirst("workshopId")?.Value ??
                User.FindFirst("WorkshopId")?.Value;

            if (!int.TryParse(workshopIdClaim, out var workshopId) || workshopId <= 0)
            {
                return ServiceResult<int>.Fail(
                    "Workshop bilgisi bulunamadı.",
                    HttpStatusCode.Unauthorized);
            }

            return ServiceResult<int>.Success(workshopId);
        }

        protected ServiceResult<int> GetCurrentUserId()
        {
            var userIdClaim =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                User.FindFirst("userId")?.Value ??
                User.FindFirst("UserId")?.Value ??
                User.FindFirst("sub")?.Value;

            if (!int.TryParse(userIdClaim, out var userId) || userId <= 0)
            {
                return ServiceResult<int>.Fail(
                    "Kullanıcı bilgisi bulunamadı.",
                    HttpStatusCode.Unauthorized);
            }

            return ServiceResult<int>.Success(userId);
        }

        protected string? GetCurrentUserRole()
        {
            return User.FindFirst(ClaimTypes.Role)?.Value ??
                   User.FindFirst("role")?.Value ??
                   User.FindFirst("Role")?.Value;
        }

        protected string? GetEditLockToken()
        {
            var value = Request.Headers[EditLockTokenHeaderName].FirstOrDefault();
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        protected int? GetServiceRecordIdHeader()
        {
            var value = Request.Headers[ServiceRecordIdHeaderName].FirstOrDefault();
            return int.TryParse(value, out var serviceRecordId) && serviceRecordId > 0
                ? serviceRecordId
                : null;
        }

        protected IActionResult ToActionResult<T>(ServiceResult<T> result)
        {
            return StatusCode((int)result.StatusCode, result);
        }

        protected IActionResult UnauthorizedResult<T>(ServiceResult<T> result)
        {
            return StatusCode((int)HttpStatusCode.Unauthorized, result);
        }
    }
}
