using AutoStock.WEB.Services;
using Microsoft.AspNetCore.Mvc;

namespace AutoStock.WEB.Controllers
{
    public class VehicleQrCodesController : BaseController
    {
        private readonly VehicleQrCodesApiService _vehicleQrCodesApiService;

        public VehicleQrCodesController(VehicleQrCodesApiService vehicleQrCodesApiService)
        {
            _vehicleQrCodesApiService = vehicleQrCodesApiService;
        }

        [HttpGet("VehicleQrCodes/Scan")]
        public IActionResult Scan()
        {
            var token = HttpContext.Session.GetString("AuthToken");

            if (string.IsNullOrWhiteSpace(token))
                return RedirectToLogin();

            if (IsAdmin)
                return RedirectToAction("Dashboard", "Admin");

            ViewBag.Role = HttpContext.Session.GetString("UserRole") ?? "";
            ViewBag.FullName = HttpContext.Session.GetString("FullName") ?? "";

            return View();
        }

        [HttpGet("VehicleQrCodes/Resolve")]
        public async Task<IActionResult> Resolve([FromQuery] string code)
        {
            var token = HttpContext.Session.GetString("AuthToken");

            if (string.IsNullOrWhiteSpace(token))
                return Unauthorized(new { errorMessage = "Oturum bulunamadı." });

            if (string.IsNullOrWhiteSpace(code))
                return BadRequest(new { errorMessage = "QR kod okunamadı." });

            var normalizedCode = NormalizeQrCode(code);

            var result = await _vehicleQrCodesApiService.ResolveAsync(normalizedCode);

            if (result.IsFailure || result.Data == null)
            {
                return BadRequest(new
                {
                    errorMessage = result.ErrorMessage ?? "QR kod çözümlenemedi.",
                    errorMessages = result.ErrorMessages
                });
            }

            return Json(result.Data);
        }

        private static string NormalizeQrCode(string? code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return string.Empty;

            code = code.Trim();

            if (Uri.TryCreate(code, UriKind.Absolute, out var uri))
            {
                code = uri.Segments.LastOrDefault()?.Trim('/') ?? code;
            }

            return code.Trim();
        }
    }
}