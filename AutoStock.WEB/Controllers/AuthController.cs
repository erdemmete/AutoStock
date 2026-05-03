using AutoStock.WEB.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
using static System.Net.WebRequestMethods;

namespace AutoStock.WEB.Controllers
{
    public class AuthController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public AuthController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult Login()
        {
            var token = HttpContext.Session.GetString("AuthToken");

            if (!string.IsNullOrWhiteSpace(token))
            {
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
                    ViewBag.Error = "API adresi yapılandırılmamış. Lütfen sistem yöneticisiyle iletişime geçin.";
                    return View(model);
                }

                var client = _httpClientFactory.CreateClient();

                var json = JsonSerializer.Serialize(model);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"{apiBaseUrl}/api/Auth/login", content);

                var responseText = await response.Content.ReadAsStringAsync();

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    ViewBag.Error = "Kullanıcı adı/e-posta veya şifre hatalı.";
                    return View(model);
                }

                if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    ViewBag.Error = string.IsNullOrWhiteSpace(responseText)
                        ? "Giriş bilgileri eksik veya hatalı."
                        : responseText;

                    return View(model);
                }

                if (!response.IsSuccessStatusCode)
                {
                    ViewBag.Error = $"API hatası oluştu. Status: {(int)response.StatusCode} - {response.ReasonPhrase}";
                    return View(model);
                }

                if (string.IsNullOrWhiteSpace(responseText))
                {
                    ViewBag.Error = "API boş cevap döndü.";
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
                    ViewBag.Error = "Token alınamadı.";
                    return View(model);
                }

                HttpContext.Session.SetString("AuthToken", loginResult.AccessToken);
                HttpContext.Session.SetString("UserRole", loginResult.Role);
                HttpContext.Session.SetString("FullName", loginResult.FullName);
                HttpContext.Session.SetInt32("UserId", loginResult.UserId);
                HttpContext.Session.SetInt32("WorkshopId", loginResult.WorkshopId);

                if (loginResult.Role == "Admin")
                {
                    return RedirectToAction("Index", "Admin");
                }

                return RedirectToAction("Index", "Dashboard");
            }
            catch (HttpRequestException)
            {
                ViewBag.Error = "API sunucusuna ulaşılamıyor. API çalışıyor mu kontrol et.";
                return View(model);
            }
            catch (TaskCanceledException)
            {
                ViewBag.Error = "API isteği zaman aşımına uğradı.";
                return View(model);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Beklenmeyen hata: {ex.Message}";
                return View(model);
            }
        }

        [HttpPost]
        public IActionResult Logout()
        {
            HttpContext.Session.Remove("AuthToken");
            return RedirectToAction("Login", "Auth");
        }
    }
}