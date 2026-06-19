using AutoStock.Services.Dtos.Notifications;
using AutoStock.WEB.Services;
using Microsoft.AspNetCore.Mvc;

namespace AutoStock.WEB.Controllers
{
    public class NotificationsController : BaseController
    {
        private readonly NotificationApiService _notificationApiService;

        public NotificationsController(NotificationApiService notificationApiService)
        {
            _notificationApiService = notificationApiService;
        }

        [HttpGet("Notifications")]
        public async Task<IActionResult> Index([FromQuery] NotificationListQueryDto query)
        {
            if (CurrentUserId is null)
                return RedirectToLogin();

            query ??= new NotificationListQueryDto();
            query.Normalize();

            var result = await _notificationApiService.GetPagedAsync(query);

            if (result.IsFailure || result.Data is null)
            {
                ShowError(result.ErrorMessage ?? "Bildirimler alınırken hata oluştu.");
            }

            ViewBag.Query = query;

            return View(result.Data ?? new AutoStock.Services.Dtos.Common.PagedResult<NotificationListItemDto>
            {
                PageNumber = query.PageNumber,
                PageSize = query.PageSize
            });
        }

        [HttpGet("Notifications/Header")]
        public IActionResult Header()
        {
            if (CurrentUserId is null)
                return Unauthorized();

            return ViewComponent("NotificationBell");
        }

        [HttpPost("Notifications/{id:int}/Read")]
        public async Task<IActionResult> Read(int id, string? returnUrl)
        {
            if (CurrentUserId is null)
                return RedirectToLogin();

            await _notificationApiService.MarkAsReadAsync(id);

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction(nameof(Index));
        }

        [HttpPost("Notifications/ReadAll")]
        public async Task<IActionResult> ReadAll(string? returnUrl)
        {
            if (CurrentUserId is null)
                return RedirectToLogin();

            await _notificationApiService.MarkAllAsReadAsync();

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction(nameof(Index));
        }
    }
}
