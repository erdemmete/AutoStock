using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AutoStock.WEB.Controllers
{
    [Route("quick-offer-pdf")]
    public class QuickOfferPdfProxyController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public QuickOfferPdfProxyController(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] object request)
        {
            var token = HttpContext.Session.GetString("AuthToken");

            if (string.IsNullOrWhiteSpace(token))
                return Unauthorized("Oturum bulunamadi. Lutfen tekrar giris yapin.");

            var apiBaseUrl = _configuration["ApiSettings:BaseUrl"];

            var client = _httpClientFactory.CreateClient();

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var enrichedRequestJson = JsonSerializer.Serialize(request);

            var content = new StringContent(
                enrichedRequestJson,
                Encoding.UTF8,
                "application/json");

            var response = await client.PostAsync(
                $"{apiBaseUrl}/api/QuickOfferPdfs",
                content);

            var fileBytes = await response.Content.ReadAsByteArrayAsync();

            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());

            var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
                ?? response.Content.Headers.ContentDisposition?.FileName
                ?? "hizli-teklif.pdf";

            fileName = fileName.Trim('"');

            return File(fileBytes, "application/pdf", fileName);
        }
    }
}