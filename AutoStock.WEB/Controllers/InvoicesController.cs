using AutoStock.WEB.Models.Common;
using AutoStock.WEB.Models.Invoices;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AutoStock.WEB.Controllers;

public class InvoicesController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public InvoicesController(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    [HttpGet("Invoices/CreateFromServiceRecord/{serviceRecordId:int}")]
    public async Task<IActionResult> CreateFromServiceRecord(int serviceRecordId)
    {
        var client = CreateApiClient();

        var response = await client.GetAsync(
            $"/api/invoices/draft/from-service-record/{serviceRecordId}");

        if (!response.IsSuccessStatusCode)
        {
            TempData["ErrorMessage"] = "Fatura taslağı oluşturulamadı.";
            return RedirectToAction("Detail", "ServiceRecords", new { id = serviceRecordId });
        }

        var json = await response.Content.ReadAsStringAsync();

        var result = JsonSerializer.Deserialize<ApiResponse<InvoiceCreateViewModel>>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (result is null || !result.IsSuccess || result.Data is null)
        {
            TempData["ErrorMessage"] = result?.ErrorMessage ?? "Fatura taslağı okunamadı.";
            return RedirectToAction("Detail", "ServiceRecords", new { id = serviceRecordId });
        }

        return View(result.Data);
    }

    [HttpPost("Invoices/CreateFromServiceRecord")]
    public async Task<IActionResult> CreateFromServiceRecord([FromBody] InvoiceCreateViewModel model)
    {
        if (model.Items is null || !model.Items.Any())
        {
            return BadRequest(new
            {
                isSuccess = false,
                errorMessage = new[] { "Fatura kalemi zorunludur." }
            });
        }

        var client = CreateApiClient();

        var json = JsonSerializer.Serialize(model);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/api/invoices", content);

        var responseJson = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            return StatusCode((int)response.StatusCode, responseJson);

        return Content(responseJson, "application/json");
    }

    private HttpClient CreateApiClient()
    {
        var client = _httpClientFactory.CreateClient();

        client.BaseAddress = new Uri(_configuration["ApiSettings:BaseUrl"]!);

        var token = HttpContext.Session.GetString("AuthToken");

        if (!string.IsNullOrWhiteSpace(token))
        {
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }

        return client;
    }
}