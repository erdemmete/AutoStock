using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;

namespace AutoStock.WEB.Controllers
{
    [Route("service-pdf")]
    public class ServicePdfProxyController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public ServicePdfProxyController(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        [HttpGet("create/{serviceRecordId:int}")]
        public async Task<IActionResult> Create(int serviceRecordId)
        {
            var token = HttpContext.Session.GetString("AuthToken");

            if (string.IsNullOrWhiteSpace(token))
                return Unauthorized("Oturum bulunamadı. Lütfen tekrar giriş yapın.");

            var apiBaseUrl = _configuration["ApiSettings:BaseUrl"];

            var client = _httpClientFactory.CreateClient();

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync(
                $"{apiBaseUrl}/api/ServicePdfs/{serviceRecordId}");

            var fileBytes = await response.Content.ReadAsByteArrayAsync();

            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode);
            var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
    ?? response.Content.Headers.ContentDisposition?.FileName
    ?? $"servis-formu-{serviceRecordId}.pdf";

            fileName = fileName.Trim('"');

            return File(fileBytes, "application/pdf", fileName); return File(fileBytes, "application/pdf", $"servis-formu-{serviceRecordId}.pdf");
        }
    }
}