using AutoStock.Services.Dtos.Notifications;
using AutoStock.WEB.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AutoStock.WEB.ViewComponents
{
    public class NotificationBellViewComponent : ViewComponent
    {
        private readonly NotificationApiService _notificationApiService;

        public NotificationBellViewComponent(NotificationApiService notificationApiService)
        {
            _notificationApiService = notificationApiService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            try
            {
                var token = HttpContext.Session.GetString("AuthToken");

                if (string.IsNullOrWhiteSpace(token))
                {
                    return View(new NotificationHeaderDto());
                }

                var result = await _notificationApiService.GetHeaderAsync(8);

                return View(result.Data ?? new NotificationHeaderDto());
            }
            catch
            {
                return View(new NotificationHeaderDto());
            }
        }
    }
}