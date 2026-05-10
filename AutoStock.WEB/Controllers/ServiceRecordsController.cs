using AutoStock.Web.Models.ServiceRecords;
using AutoStock.WEB.Models.Common;
using AutoStock.WEB.Models.ServiceRecords;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AutoStock.Web.Controllers;


public class ServiceRecordsController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public ServiceRecordsController(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;

    }

    [HttpGet("ServiceRecords")]
    public async Task<IActionResult> Index()
    {
        var client = CreateApiClient();

        var response = await client.GetAsync("/api/ServiceRecords");

        if (!response.IsSuccessStatusCode)
        {
            ViewBag.ErrorMessage = "Servis kayıtları getirilemedi.";
            return View(new List<ServiceRecordListItemViewModel>());
        }

        var json = await response.Content.ReadAsStringAsync();

        var result = JsonSerializer.Deserialize<ApiResponse<List<ServiceRecordListItemViewModel>>>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return View(result?.Data ?? new List<ServiceRecordListItemViewModel>());
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new CreateServiceRecordViewModel
        {
            Brands = await GetBrandsAsync()
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateServiceRecordViewModel model)
    {
        model.Brands = await GetBrandsAsync();

        if (!ModelState.IsValid)
            return View(model);

        var client = CreateApiClient();

        var json = JsonSerializer.Serialize(model);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/api/ServiceRecords", content);

        var responseJson = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            ModelState.AddModelError("", "Servis kaydı oluşturulurken hata oluştu.");
            return View(model);
        }

        var result = JsonSerializer.Deserialize<ApiResponse<CreateServiceRecordResponseViewModel>>(
            responseJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (result is null || !result.IsSuccess || result.Data is null)
        {
            ModelState.AddModelError("", result?.ErrorMessage ?? "Servis kaydı oluşturulamadı.");
            return View(model);
        }

        TempData["SuccessMessage"] = $"Servis kaydı oluşturuldu. Kayıt No: {result.Data.RecordNumber}";

        return RedirectToAction(nameof(Create));
    }

    [HttpGet]
    public async Task<IActionResult> GetModels(int brandId)
    {
        var client = CreateApiClient();

        var response = await client.GetAsync($"/api/VehicleCatalog/brands/{brandId}/models");

        if (!response.IsSuccessStatusCode)
            return Json(new List<VehicleModelViewModel>());

        var json = await response.Content.ReadAsStringAsync();

        var models = JsonSerializer.Deserialize<List<VehicleModelViewModel>>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return Json(models ?? new List<VehicleModelViewModel>());
    }
    [HttpGet("ServiceRecords/Detail/{id:int}")]
    public async Task<IActionResult> Detail(int id)
    {
        var client = CreateApiClient();

        var response = await client.GetAsync($"/api/ServiceRecords/{id}");

        if (!response.IsSuccessStatusCode)
        {
            TempData["ErrorMessage"] = "Servis kaydı bulunamadı.";
            return RedirectToAction(nameof(Index));
        }

        var json = await response.Content.ReadAsStringAsync();

        var result = JsonSerializer.Deserialize<ApiResponse<ServiceRecordDetailViewModel>>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (result?.Data is null)
        {
            TempData["ErrorMessage"] = "Servis kaydı okunamadı.";
            return RedirectToAction(nameof(Index));
        }

        return View(result.Data);
    }

    [HttpGet("ServiceRecords/SearchCustomers")]
    public async Task<IActionResult> SearchCustomers(string query)
    {
        var client = CreateApiClient();

        var response = await client.GetAsync($"/api/Customers/search?query={Uri.EscapeDataString(query)}");

        if (!response.IsSuccessStatusCode)
            return Json(new List<object>());

        var json = await response.Content.ReadAsStringAsync();

        return Content(json, "application/json");
    }

    private async Task<List<VehicleBrandViewModel>> GetBrandsAsync()
    {
        var client = CreateApiClient();

        var response = await client.GetAsync("/api/VehicleCatalog/brands");

        if (!response.IsSuccessStatusCode)
            return new List<VehicleBrandViewModel>();

        var json = await response.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<List<VehicleBrandViewModel>>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<VehicleBrandViewModel>();
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
    [HttpPost("ServiceRecords/UpdateRequestItem")]
    public async Task<IActionResult> UpdateRequestItem(UpdateServiceRequestItemViewModel model)
    {
        var client = CreateApiClient();

        var requestBody = new
        {
            repairDetail = model.RepairDetail,
            finalAmount = model.FinalAmount,
            isResolved = model.IsResolved
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PutAsync(
            $"/api/ServiceRecords/request-items/{model.RequestItemId}",
            content);

        if (!response.IsSuccessStatusCode)
        {
            TempData["ErrorMessage"] = "Talep güncellenirken hata oluştu.";
        }
        else
        {
            TempData["SuccessMessage"] = "Talep başarıyla güncellendi.";
        }

        return RedirectToAction(nameof(Detail), new { id = model.ServiceRecordId });
    }

}