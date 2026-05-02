using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

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

            [HttpPost("create")]
            public async Task<IActionResult> Create([FromBody] object request)
            {
            try
            {
                var apiBaseUrl = _configuration["ApiSettings:BaseUrl"];

                if (string.IsNullOrWhiteSpace(apiBaseUrl))
                    return StatusCode(500, "ApiSettings:BaseUrl bulunamadı.");

                var client = _httpClientFactory.CreateClient();

                var json = JsonSerializer.Serialize(request);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(
                    $"{apiBaseUrl}/api/ServicePdfs",
                    content
                );

                var responseText = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return StatusCode((int)response.StatusCode, responseText);

                var pdfBytes = await response.Content.ReadAsByteArrayAsync();

                return File(pdfBytes, "application/pdf", "servis-formu.pdf");
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.ToString());
            }
        }
        }
   
}

