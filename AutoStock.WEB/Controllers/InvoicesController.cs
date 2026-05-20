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

    [HttpGet("Invoices/Print/{invoiceId:int}")]
    public async Task<IActionResult> Print(int invoiceId)
    {
        var client = CreateApiClient();

        var response = await client.GetAsync($"/api/invoices/{invoiceId}");

        if (!response.IsSuccessStatusCode)
        {
            TempData["ErrorMessage"] = "Fatura bulunamadı.";
            return RedirectToAction(nameof(Index));
        }

        var json = await response.Content.ReadAsStringAsync();

        var result = JsonSerializer.Deserialize<ApiResponse<InvoiceDetailViewModel>>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (result is null || !result.IsSuccess || result.Data is null)
        {
            TempData["ErrorMessage"] = result?.ErrorMessage ?? "Fatura detayı okunamadı.";
            return RedirectToAction(nameof(Index));
        }

        return View(result.Data);
    }
    

    [HttpGet("Invoices/Detail/{invoiceId:int}")]
    public async Task<IActionResult> Detail(int invoiceId)
    {
        var client = CreateApiClient();

        var response = await client.GetAsync($"/api/invoices/{invoiceId}");

        if (!response.IsSuccessStatusCode)
        {
            TempData["ErrorMessage"] = "Fatura bulunamadı.";
            return RedirectToAction("Index", "Dashboard");
        }

        var json = await response.Content.ReadAsStringAsync();

        var result = JsonSerializer.Deserialize<ApiResponse<InvoiceDetailViewModel>>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (result is null || !result.IsSuccess || result.Data is null)
        {
            TempData["ErrorMessage"] = result?.ErrorMessage ?? "Fatura detayı okunamadı.";
            return RedirectToAction("Index", "Dashboard");
        }

        return View(result.Data);
    }

    [HttpPost("Invoices/Issue/{invoiceId:int}")]
    public async Task<IActionResult> Issue(int invoiceId)
    {
        var client = CreateApiClient();

        var response = await client.PostAsync($"/api/invoices/{invoiceId}/issue", null);

        var json = await response.Content.ReadAsStringAsync();

        return Content(json, "application/json");
    }

    [HttpGet("Invoices")]
    public async Task<IActionResult> Index()
    {
        var client = CreateApiClient();

        var response = await client.GetAsync("/api/invoices");

        if (!response.IsSuccessStatusCode)
        {
            ViewBag.ErrorMessage = "Faturalar getirilemedi.";
            return View(new List<InvoiceListItemViewModel>());
        }

        var json = await response.Content.ReadAsStringAsync();

        var result = JsonSerializer.Deserialize<ApiResponse<List<InvoiceListItemViewModel>>>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return View(result?.Data ?? new List<InvoiceListItemViewModel>());
    }

    [HttpPost]
    public async Task<IActionResult> Cancel(int id)
    {
       

        var client = CreateApiClient();

        var response = await client.PostAsync($"/api/invoices/{id}/cancel", null);

        if (!response.IsSuccessStatusCode)
            return BadRequest();

        return Ok();
    }

    [HttpGet("Invoices/Edit/{invoiceId:int}")]
    public async Task<IActionResult> Edit(int invoiceId)
    {
        var client = CreateApiClient();

        var response = await client.GetAsync($"/api/invoices/{invoiceId}");

        if (!response.IsSuccessStatusCode)
        {
            TempData["ErrorMessage"] = "Fatura bulunamadı.";
            return RedirectToAction(nameof(Index));
        }

        var json = await response.Content.ReadAsStringAsync();

        var result = JsonSerializer.Deserialize<ApiResponse<InvoiceDetailViewModel>>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (result is null || !result.IsSuccess || result.Data is null)
        {
            TempData["ErrorMessage"] = result?.ErrorMessage ?? "Fatura detayı okunamadı.";
            return RedirectToAction(nameof(Index));
        }

        if (result.Data.Status != 1)
        {
            TempData["ErrorMessage"] = "Sadece taslak faturalar düzenlenebilir.";
            return RedirectToAction(nameof(Detail), new { invoiceId });
        }

        return View(result.Data);
    }

    [HttpPut("Invoices/Edit/{invoiceId:int}")]
    public async Task<IActionResult> Edit(int invoiceId, [FromBody] InvoiceDetailViewModel model)
    {
        var client = CreateApiClient();

        var json = JsonSerializer.Serialize(model);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PutAsync($"/api/invoices/{invoiceId}", content);

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