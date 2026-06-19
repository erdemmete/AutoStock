using AutoStock.Services.Dtos.WebPush;
using AutoStock.WEB.Services;
using Microsoft.AspNetCore.Mvc;

namespace AutoStock.WEB.Controllers
{
    [Route("WebPush")]
    public class WebPushController : BaseController
    {
        private readonly WebPushApiService _webPushApiService;

        public WebPushController(WebPushApiService webPushApiService)
        {
            _webPushApiService = webPushApiService;
        }

        [HttpGet("PublicKey")]
        public async Task<IActionResult> PublicKey()
        {
            if (CurrentUserId is null)
                return Unauthorized();

            var result = await _webPushApiService.GetPublicKeyAsync();

            if (result.IsFailure || string.IsNullOrWhiteSpace(result.Data))
                return BadRequest(new { message = result.ErrorMessage ?? "Tarayıcı bildirimleri yapılandırılmamış." });

            return Json(new { publicKey = result.Data });
        }

        [HttpPost("Status")]
        public async Task<IActionResult> Status([FromBody] WebPushSubscriptionRequestDto request)
        {
            if (CurrentUserId is null)
                return Unauthorized();

            var result = await _webPushApiService.GetStatusAsync(request ?? new WebPushSubscriptionRequestDto());

            if (result.IsFailure)
                return BadRequest(new { message = result.ErrorMessage ?? "Tarayıcı bildirimi durumu alınamadı." });

            return Json(result.Data);
        }

        [HttpPost("Subscribe")]
        public async Task<IActionResult> Subscribe([FromBody] WebPushSubscriptionRequestDto request)
        {
            if (CurrentUserId is null)
                return Unauthorized();

            var result = await _webPushApiService.SubscribeAsync(request);

            if (result.IsFailure)
                return BadRequest(new { message = result.ErrorMessage ?? "Tarayıcı bildirimi açılamadı." });

            return Json(new { success = true });
        }

        [HttpPost("Unsubscribe")]
        public async Task<IActionResult> Unsubscribe([FromBody] WebPushSubscriptionRequestDto request)
        {
            if (CurrentUserId is null)
                return Unauthorized();

            var result = await _webPushApiService.UnsubscribeAsync(request ?? new WebPushSubscriptionRequestDto());

            if (result.IsFailure)
                return BadRequest(new { message = result.ErrorMessage ?? "Tarayıcı bildirimi kapatılamadı." });

            return Json(new { success = true });
        }
    }
}
