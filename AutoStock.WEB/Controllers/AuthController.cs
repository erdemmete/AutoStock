using AutoStock.WEB.Models;
using AutoStock.WEB.Models.Auth;
using AutoStock.WEB.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace AutoStock.WEB.Controllers
{
    public class AuthController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly AuthApiService _authApiService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            AuthApiService authApiService,
            ILogger<AuthController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _authApiService = authApiService;
            _logger = logger;
        }

        [HttpGet("/Auth/Login")]
        public IActionResult Login()
        {
            var token = HttpContext.Session.GetString("AuthToken");
 
            if (!string.IsNullOrWhiteSpace(token))
            {
                var role = HttpContext.Session.GetString("UserRole");

                if (role == "Admin")
                {
                    return RedirectToAction("Dashboard", "Admin");
                }

                return RedirectToAction("Index", "Dashboard");
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var apiBaseUrl = _configuration["ApiSettings:BaseUrl"];// "http://localhost:5000";

                if (string.IsNullOrWhiteSpace(apiBaseUrl))
                {
                    _logger.LogError("Login failed because ApiSettings:BaseUrl is missing.");
                    ViewBag.Error = "Giriş şu anda tamamlanamadı. Lütfen kısa süre sonra tekrar deneyin.";
                    return View(model);
                }

                var client = _httpClientFactory.CreateClient();

                var json = JsonSerializer.Serialize(model);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"{apiBaseUrl}/api/Auth/login", content);

                var responseText = await response.Content.ReadAsStringAsync();

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    ViewBag.Error = "Kullanıcı adı veya şifre hatalı.";
                    return View(model);
                }

                if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    _logger.LogWarning(
                        "Login API returned BadRequest. Response: {ResponseText}",
                        responseText);

                    ViewBag.Error = "Kullanıcı adı veya şifre hatalı.";

                    return View(model);
                }

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError(
                        "Login API returned unsuccessful status. StatusCode: {StatusCode}, Reason: {ReasonPhrase}, Response: {ResponseText}",
                        (int)response.StatusCode,
                        response.ReasonPhrase,
                        responseText);

                    ViewBag.Error = "Giriş şu anda tamamlanamadı. Lütfen kısa süre sonra tekrar deneyin.";
                    return View(model);
                }

                if (string.IsNullOrWhiteSpace(responseText))
                {
                    _logger.LogError("Login API returned an empty response.");
                    ViewBag.Error = "Giriş şu anda tamamlanamadı. Lütfen kısa süre sonra tekrar deneyin.";
                    return View(model);
                }

                var loginResult = JsonSerializer.Deserialize<AuthResponseViewModel>(
                    responseText,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                if (loginResult == null || string.IsNullOrWhiteSpace(loginResult.AccessToken))
                {
                    _logger.LogError("Login API response could not be parsed or access token was missing.");
                    ViewBag.Error = "Giriş şu anda tamamlanamadı. Lütfen kısa süre sonra tekrar deneyin.";
                    return View(model);
                }

                StoreAuthSession(loginResult);

                return RedirectAfterLogin(loginResult);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Login API request failed.");
                ViewBag.Error = "Giriş şu anda tamamlanamadı. Lütfen kısa süre sonra tekrar deneyin.";
                return View(model);
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Login API request timed out.");
                ViewBag.Error = "Giriş şu anda tamamlanamadı. Lütfen kısa süre sonra tekrar deneyin.";
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred during login.");
                ViewBag.Error = "Giriş sırasında bir sorun oluştu.";
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();

            return RedirectToAction("Login", "Auth");
        }

        [HttpGet("/Auth/PasswordSetup")]
        public async Task<IActionResult> PasswordSetup([FromQuery] string token)
        {
            var model = new PasswordSetupViewModel
            {
                Token = token
            };

            if (string.IsNullOrWhiteSpace(token))
            {
                model.ErrorMessage = "Kurulum bağlantısı geçersiz.";
                return View(model);
            }

            var result = await _authApiService.ValidatePasswordSetupTokenAsync(token);

            if (result.IsFailure || result.Data == null)
            {
                model.ErrorMessage = result.ErrorMessage ?? "Kurulum bağlantısı geçersiz veya süresi dolmuş.";
                return View(model);
            }

            model.FullName = result.Data.FullName;
            model.UserName = result.Data.UserName;
            model.WorkshopName = result.Data.WorkshopName;

            return View(model);
        }

        [HttpPost("/Auth/PasswordSetup")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PasswordSetup(PasswordSetupViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _authApiService.CompletePasswordSetupAsync(model);

            if (result.IsFailure || result.Data == null)
            {
                model.ErrorMessage = result.ErrorMessage ?? "Şifre oluşturulurken hata oluştu.";
                return View(model);
            }

            StoreAuthSession(result.Data);

            return RedirectAfterLogin(result.Data);
        }

        [HttpGet("/Auth/InviteCode")]
        public IActionResult InviteCode()
        {
            return View(new InviteCodeViewModel());
        }

        [HttpPost("/Auth/InviteCode")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InviteCode(InviteCodeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _authApiService.CompletePasswordSetupByCodeAsync(model);

            if (result.IsFailure || result.Data == null)
            {
                model.ErrorMessage = result.ErrorMessage ?? "Davet kodu geçersiz veya süresi dolmuş.";
                return View(model);
            }

            StoreAuthSession(result.Data);

            return RedirectAfterLogin(result.Data);
        }

        [HttpGet("/Auth/PasswordReset")]
        public async Task<IActionResult> PasswordReset([FromQuery] string token)
        {
            var model = new PasswordResetViewModel
            {
                Token = token
            };

            if (string.IsNullOrWhiteSpace(token))
            {
                model.ErrorMessage = "Şifre sıfırlama bağlantısı geçersiz.";
                return View(model);
            }

            var result = await _authApiService.ValidatePasswordResetTokenAsync(token);

            if (result.IsFailure || result.Data == null)
            {
                model.ErrorMessage = result.ErrorMessage
                    ?? "Şifre sıfırlama bağlantısı geçersiz, kullanılmış veya süresi dolmuş.";

                return View(model);
            }

            model.FullName = result.Data.FullName;
            model.UserName = result.Data.UserName;
            model.WorkshopName = result.Data.WorkshopName;

            return View(model);
        }

        [HttpPost("/Auth/PasswordReset")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PasswordReset(PasswordResetViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _authApiService.CompletePasswordResetAsync(model);

            if (result.IsFailure || result.Data == null)
            {
                model.ErrorMessage = result.ErrorMessage
                    ?? "Şifre sıfırlanırken hata oluştu.";

                return View(model);
            }

            StoreAuthSession(result.Data);

            return RedirectAfterLogin(result.Data);
        }

        private void StoreAuthSession(AuthResponseViewModel authResponse)
        {
            HttpContext.Session.SetString("AuthToken", authResponse.AccessToken);
            HttpContext.Session.SetString("UserRole", authResponse.Role);
            HttpContext.Session.SetString("FullName", authResponse.FullName);
            HttpContext.Session.SetInt32("UserId", authResponse.UserId);
            HttpContext.Session.SetInt32("WorkshopId", authResponse.WorkshopId);
        }

        private IActionResult RedirectAfterLogin(AuthResponseViewModel authResponse)
        {
            if (authResponse.Role == "Admin")
            {
                return RedirectToAction("Dashboard", "Admin");
            }

            return RedirectToAction("Index", "Dashboard");
        }
    }
}
