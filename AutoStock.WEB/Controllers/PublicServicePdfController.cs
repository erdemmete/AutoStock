using Microsoft.AspNetCore.Mvc;

namespace AutoStock.WEB.Controllers
{
    public class PublicServicePdfController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public PublicServicePdfController(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        [HttpGet("/public/service-pdf/{qrCode}/{serviceRecordId:int}")]
        public async Task<IActionResult> Get(string qrCode, int serviceRecordId)
        {
            var client = _httpClientFactory.CreateClient();

            var apiBaseUrl = _configuration["ApiSettings:BaseUrl"];

            var response = await client.GetAsync(
                $"{apiBaseUrl}/public/service-pdf/{qrCode}/{serviceRecordId}");

            var responseBytes = await response.Content.ReadAsByteArrayAsync();

            if (!response.IsSuccessStatusCode)
            {
                var errorText = System.Text.Encoding.UTF8.GetString(responseBytes);
                return BadRequest(errorText);
            }

            return File(responseBytes, "application/pdf");
        }
    }
}