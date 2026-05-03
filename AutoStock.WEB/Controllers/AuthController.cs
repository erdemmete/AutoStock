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
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            var apiBaseUrl = _configuration["ApiSettings:BaseUrl"]
                        ?? throw new Exception("ApiSettings:BaseUrl missing!");

            var client = _httpClientFactory.CreateClient();

            var json = JsonSerializer.Serialize(model);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync($"{apiBaseUrl}/api/Auth/login", content);

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "Email veya şifre hatalı.";
                return View(model);
            }

            var responseText = await response.Content.ReadAsStringAsync();

            HttpContext.Session.SetString("AuthToken", responseText);

            return RedirectToAction("Index", "Home");
        }
    }
}