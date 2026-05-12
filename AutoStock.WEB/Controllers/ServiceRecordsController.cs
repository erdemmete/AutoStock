using AutoStock.Services.Dtos.Common;
using AutoStock.Services.Dtos.ServiceRecords;
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
            Brands = await GetBrandsAsync(),

            ServiceAdvisorName = HttpContext.Session.GetString("FullName") ?? "Oturum Bilgisi Yok"
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateServiceRecordViewModel model)
    {
        model.Brands = await GetBrandsAsync();

        model.ServiceAdvisorName = HttpContext.Session.GetString("FullName") ?? "Oturum Bilgisi Yok";

        if (!ModelState.IsValid)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Any())
                    .ToDictionary(
                        x => x.Key,
                        x => x.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    );

                return BadRequest(new
                {
                    success = false,
                    validationErrors = errors
                });
            }

            return View(model);
        }


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

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return Json(new
            {
                success = true,
                message = "Servis kaydı başarıyla oluşturuldu.",
                serviceRecordId = result.Data.ServiceRecordId,
                recordNumber = result.Data.RecordNumber
            });
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

    [HttpPost("ServiceRecords/AssignQrCode")]
    public async Task<IActionResult> AssignQrCode(int vehicleId, string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return BadRequest("QR kod zorunludur.");

        var client = CreateApiClient();

        var requestBody = new
        {
            vehicleId,
            code = code.Trim()
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/api/VehicleQrCodes/assign", content);

        var responseText = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            return BadRequest(responseText);

        return Content(responseText, "application/json");
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

    [HttpPost("ServiceRecords/AddRequestItem")]
    public async Task<IActionResult> AddRequestItem(CreateServiceRequestItemViewModel model, int serviceRecordId)
    {
        var client = CreateApiClient();

        var requestBody = new
        {
            title = model.Title,
            note = model.Note,
            estimatedAmount = model.EstimatedAmount
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync(
            $"/api/ServiceRecords/{serviceRecordId}/request-items",
            content);

        if (!response.IsSuccessStatusCode)
            return BadRequest();

        var responseJson = await response.Content.ReadAsStringAsync();

        var apiResult = JsonSerializer.Deserialize<ApiResponse<int>>(
            responseJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return Json(new
        {
            id = apiResult?.Data,
            title = model.Title,
            note = model.Note,
            estimatedAmount = model.EstimatedAmount
        });
    }

    [HttpPost("ServiceRecords/AddOperation")]
    public async Task<IActionResult> AddOperation(AddServiceOperationViewModel model)
    {
        var client = CreateApiClient();

        var requestBody = new
        {
            serviceRequestItemId = model.ServiceRequestItemId,
            type = model.Type,
            description = model.Description,
            quantity = model.Quantity,
            unitPrice = model.UnitPrice,
            note = model.Note
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync(
            $"/api/ServiceRecords/{model.ServiceRecordId}/operations",
            content);

        var responseJson = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            return BadRequest(responseJson);

        var result = JsonSerializer.Deserialize<ServiceResult<ServiceOperationDto>>(
            responseJson,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        if (result == null || !result.IsSuccess || result.Data == null)
            return BadRequest(responseJson);

        return Json(result.Data);
    }

    [HttpPost("ServiceRecords/UpdateStatus")]
    public async Task<IActionResult> UpdateStatus(UpdateServiceRecordStatusViewModel model)
    {
        var client = CreateApiClient();

        var requestBody = new
        {
            status = model.Status
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PutAsync(
            $"/api/ServiceRecords/{model.ServiceRecordId}/status",
            content);

        if (!response.IsSuccessStatusCode)
        {
            return BadRequest();
        }

        return Ok();
    }

    [HttpPost("ServiceRecords/DeleteOperation")]
    public async Task<IActionResult> DeleteOperation(int operationId)
    {
        var client = CreateApiClient();

        var response = await client.DeleteAsync(
            $"/api/ServiceRecords/operations/{operationId}");

        if (!response.IsSuccessStatusCode)
            return BadRequest();

        var json = await response.Content.ReadAsStringAsync();

        return Content(json, "application/json");
    }

    [HttpPost("ServiceRecords/DeleteRequestItem")]
    public async Task<IActionResult> DeleteRequestItem(int requestItemId)
    {
        var client = CreateApiClient();

        var response = await client.DeleteAsync(
            $"/api/ServiceRecords/request-items/{requestItemId}");

        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            return BadRequest(json);

        return Content(json, "application/json");
    }

}