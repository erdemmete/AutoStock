using AutoStock.WEB.Models.Common;
using AutoStock.WEB.Models.Invoices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace AutoStock.WEB.Controllers
{
    [AllowAnonymous]
    public class PublicAccountSummaryController : Controller
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PublicAccountSummaryController> _logger;

        public PublicAccountSummaryController(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<PublicAccountSummaryController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet("/public/account-summary/{qrCode}/{invoiceId:int}")]
        public async Task<IActionResult> Get(string qrCode, int invoiceId)
        {
            if (string.IsNullOrWhiteSpace(qrCode) || invoiceId <= 0)
                return NotFound("Servis hesap özeti bağlantısı geçersiz.");

            var apiBaseUrl = _configuration["ApiSettings:BaseUrl"];
            if (string.IsNullOrWhiteSpace(apiBaseUrl))
                return StatusCode(StatusCodes.Status503ServiceUnavailable);

            try
            {
                var client = _httpClientFactory.CreateClient();
                var apiUrl =
                    $"{apiBaseUrl.TrimEnd('/')}/public/account-summary/{Uri.EscapeDataString(qrCode)}/{invoiceId}";
                using var response = await client.GetAsync(apiUrl);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Public account summary request failed. InvoiceId: {InvoiceId}, StatusCode: {StatusCode}",
                        invoiceId,
                        (int)response.StatusCode);

                    return NotFound("Servis hesap özeti bulunamadı veya bağlantı artık geçerli değil.");
                }

                var result = JsonSerializer.Deserialize<ApiResponse<InvoiceDetailViewModel>>(
                    content,
                    JsonOptions);

                if (result?.IsSuccess != true || result.Data is null)
                    return NotFound("Servis hesap özeti bulunamadı.");

                ViewData["IsPublicAccountSummary"] = true;
                return View("~/Views/Invoices/Print.cshtml", result.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Public account summary could not be loaded. InvoiceId: {InvoiceId}",
                    invoiceId);

                return StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    "Servis hesap özeti şu anda görüntülenemiyor. Lütfen daha sonra tekrar deneyin.");
            }
        }
    }
}
