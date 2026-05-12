using AutoStock.WEB.Models.Qr;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace AutoStock.WEB.Controllers
{
    [Route("qr")]
    public class QrController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public QrController(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        [HttpGet("{code}")]
        public async Task<IActionResult> Index(string code)
        {
            var apiBaseUrl = _configuration["ApiSettings:BaseUrl"];

            if (string.IsNullOrWhiteSpace(apiBaseUrl))
                return StatusCode(500, "ApiSettings:BaseUrl bulunamadı.");

            var client = _httpClientFactory.CreateClient();

            var response = await client.GetAsync($"{apiBaseUrl}/qr/{code}");

            if (!response.IsSuccessStatusCode)
                return View("NotFound");

            var json = await response.Content.ReadAsStringAsync();

            var model = JsonSerializer.Deserialize<PublicQrViewModel>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (model == null)
                return View("NotFound");

            return View(model);
        }
    }
}