using AutoStock.WEB.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;

namespace AutoStock.WEB.Controllers
{
    public class DashboardController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public DashboardController(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public async Task<IActionResult> Index()
        {
            var token = HttpContext.Session.GetString("AuthToken");
            var role = HttpContext.Session.GetString("UserRole");

            if (string.IsNullOrWhiteSpace(token))
            {
                return RedirectToAction("Login", "Auth");
            }

            if (role == "Admin")
            {
                return RedirectToAction("Index", "Admin");
            }

            var apiBaseUrl = _configuration["ApiSettings:BaseUrl"];

            if (string.IsNullOrWhiteSpace(apiBaseUrl))
            {
                ViewBag.Error = "API adresi bulunamadı.";
                return View(new DashboardViewModel());
            }

            var client = _httpClientFactory.CreateClient();

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync($"{apiBaseUrl}/api/Dashboard");

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "Dashboard bilgileri alınamadı.";
                return View(new DashboardViewModel());
            }

            var responseText = await response.Content.ReadAsStringAsync();

            var model = JsonSerializer.Deserialize<DashboardViewModel>(
                responseText,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            return View(model ?? new DashboardViewModel());
        }
    }
}