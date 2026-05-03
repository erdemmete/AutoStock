using AutoStock.WEB.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

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

            var apiBaseUrl = _configuration["ApiSettings:BaseUrl"]
                ?? throw new Exception("ApiSettings:BaseUrl missing!");

            var client = _httpClientFactory.CreateClient();

            var json = JsonSerializer.Serialize(model);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync($"{apiBaseUrl}/api/Auth/login", content);

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "E-posta veya şifre hatalı.";
                return View(model);
            }

            var token = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(token))
            {
                ViewBag.Error = "Token alınamadı.";
                return View(model);
            }

            HttpContext.Session.SetString("AuthToken", token);

            return RedirectToAction("Index", "Dashboard");
        }

        [HttpPost]
        public IActionResult Logout()
        {
            HttpContext.Session.Remove("AuthToken");
            return RedirectToAction("Login", "Auth");
        }
    }
}