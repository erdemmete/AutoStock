using AutoStock.Services.Constants;
using AutoStock.Services.Dtos.WebPush;
using AutoStock.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoStock.API.Controllers
{
    [ApiController]
    [Route("api/web-push")]
    [Authorize]
    public class WebPushSubscriptionsController : BaseApiController
    {
        private readonly IWebPushSubscriptionService _webPushSubscriptionService;

        public WebPushSubscriptionsController(IWebPushSubscriptionService webPushSubscriptionService)
        {
            _webPushSubscriptionService = webPushSubscriptionService;
        }

        [HttpGet("public-key")]
        public async Task<IActionResult> GetPublicKey()
        {
            var result = await _webPushSubscriptionService.GetPublicKeyAsync();
            return ToActionResult(result);
        }

        [HttpPost("status")]
        public async Task<IActionResult> GetStatus(WebPushSubscriptionRequestDto? request)
        {
            var userIdResult = GetCurrentUserId();
            if (userIdResult.IsFailure)
                return UnauthorizedResult(userIdResult);

            var workshopIdResult = GetCurrentWorkshopIdForCurrentRole();
            if (workshopIdResult.IsFailure)
                return UnauthorizedResult(workshopIdResult);

            var result = await _webPushSubscriptionService.GetStatusAsync(
                userIdResult.Data,
                workshopIdResult.Data,
                request?.Endpoint);

            return ToActionResult(result);
        }

        [HttpPost("subscribe")]
        public async Task<IActionResult> Subscribe(WebPushSubscriptionRequestDto request)
        {
            var userIdResult = GetCurrentUserId();
            if (userIdResult.IsFailure)
                return UnauthorizedResult(userIdResult);

            var workshopIdResult = GetCurrentWorkshopIdForCurrentRole();
            if (workshopIdResult.IsFailure)
                return UnauthorizedResult(workshopIdResult);

            request.UserAgent ??= Request.Headers.UserAgent.FirstOrDefault();

            var result = await _webPushSubscriptionService.UpsertAsync(
                userIdResult.Data,
                workshopIdResult.Data,
                request);

            return ToActionResult(result);
        }

        [HttpPost("unsubscribe")]
        public async Task<IActionResult> Unsubscribe(WebPushSubscriptionRequestDto request)
        {
            var userIdResult = GetCurrentUserId();
            if (userIdResult.IsFailure)
                return UnauthorizedResult(userIdResult);

            var result = await _webPushSubscriptionService.DisableAsync(
                userIdResult.Data,
                request?.Endpoint ?? string.Empty);

            return ToActionResult(result);
        }

        [HttpPost("unsubscribe-all")]
        public async Task<IActionResult> UnsubscribeAll()
        {
            var userIdResult = GetCurrentUserId();
            if (userIdResult.IsFailure)
                return UnauthorizedResult(userIdResult);

            var result = await _webPushSubscriptionService.DisableAllForUserAsync(userIdResult.Data);
            return ToActionResult(result);
        }

        private AutoStock.Services.Dtos.Common.ServiceResult<int?> GetCurrentWorkshopIdForCurrentRole()
        {
            var role = GetCurrentUserRole();

            if (string.Equals(role, AppRoles.Admin, StringComparison.OrdinalIgnoreCase))
                return AutoStock.Services.Dtos.Common.ServiceResult<int?>.Success(null);

            var workshopIdResult = GetCurrentWorkshopId();

            return workshopIdResult.IsSuccess
                ? AutoStock.Services.Dtos.Common.ServiceResult<int?>.Success(workshopIdResult.Data)
                : AutoStock.Services.Dtos.Common.ServiceResult<int?>.Fail(workshopIdResult.ErrorMessage, (System.Net.HttpStatusCode)workshopIdResult.StatusCode);
        }
    }
}
