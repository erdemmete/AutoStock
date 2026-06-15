using AutoStock.Services.Dtos.Notifications;
using AutoStock.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoStock.API.Controllers
{
    [ApiController]
    [Route("api/notifications")]
    [Authorize]
    public class NotificationsController : BaseApiController
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet("header")]
        public async Task<IActionResult> GetHeader([FromQuery] int maxItems = 8)
        {
            var userIdResult = GetCurrentUserId();

            if (userIdResult.IsFailure)
                return UnauthorizedResult(userIdResult);

            var result = await _notificationService.GetHeaderAsync(userIdResult.Data, maxItems);

            return ToActionResult(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetList([FromQuery] NotificationListQueryDto query)
        {
            var userIdResult = GetCurrentUserId();

            if (userIdResult.IsFailure)
                return UnauthorizedResult(userIdResult);

            var result = await _notificationService.GetPagedAsync(query, userIdResult.Data);

            return ToActionResult(result);
        }

        [HttpPost("{userNotificationId:int}/read")]
        public async Task<IActionResult> MarkAsRead(int userNotificationId)
        {
            var userIdResult = GetCurrentUserId();

            if (userIdResult.IsFailure)
                return UnauthorizedResult(userIdResult);

            var result = await _notificationService.MarkAsReadAsync(userNotificationId, userIdResult.Data);

            return ToActionResult(result);
        }

        [HttpPost("read-all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userIdResult = GetCurrentUserId();

            if (userIdResult.IsFailure)
                return UnauthorizedResult(userIdResult);

            var result = await _notificationService.MarkAllAsReadAsync(userIdResult.Data);

            return ToActionResult(result);
        }
    }
}
