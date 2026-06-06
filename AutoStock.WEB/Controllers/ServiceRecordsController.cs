using AutoStock.Services.Dtos.ServiceRecords;
using AutoStock.Web.Models.ServiceRecords;
using AutoStock.WEB.Controllers;
using AutoStock.WEB.Models.Invoices;
using AutoStock.WEB.Models.ServiceRecords;
using AutoStock.WEB.Models.StockItems;
using AutoStock.WEB.Services;
using Microsoft.AspNetCore.Mvc;

namespace AutoStock.Web.Controllers;

public class ServiceRecordsController : BaseController
{
    private readonly ServiceRecordApiService _serviceRecordApiService;
    private readonly ServiceRecordPageService _serviceRecordPageService;
    private readonly InvoiceApiService _invoiceApiService;

    public ServiceRecordsController(
        ServiceRecordApiService serviceRecordApiService,
        ServiceRecordPageService serviceRecordPageService,
        InvoiceApiService invoiceApiService)
    {
        _serviceRecordApiService = serviceRecordApiService;
        _serviceRecordPageService = serviceRecordPageService;
        _invoiceApiService = invoiceApiService;
    }

    [HttpGet("ServiceRecords")]
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> Index(ServiceRecordListQueryViewModel query)
    {
        var pageResult = await _serviceRecordPageService.GetIndexPageAsync(query);

        if (pageResult.HasErrors)
        {
            ShowErrors(pageResult.ErrorMessages);
        }

        return View(pageResult.ViewModel);
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
            if (IsAjaxRequest())
            {
                var errors = ModelState
                    .Where(x => x.Value?.Errors.Any() == true)
                    .ToDictionary(
                        x => x.Key,
                        x => x.Value!.Errors.Select(e => e.ErrorMessage).ToArray());

                return BadRequest(new
                {
                    success = false,
                    validationErrors = errors
                });
            }

            return View(model);
        }

        var result = await _serviceRecordApiService.CreateAsync(model);

        if (result.IsFailure || result.Data is null)
        {
            var message = result.ErrorMessage ?? "Servis kaydı oluşturulamadı.";

            if (IsAjaxRequest())
            {
                return BadRequest(new
                {
                    success = false,
                    errorMessage = message,
                    errorMessages = result.ErrorMessages
                });
            }

            ShowError(message);
            return View(model);
        }

        if (IsAjaxRequest())
        {
            return Json(new
            {
                success = true,
                message = "Servis kaydı başarıyla oluşturuldu.",
                serviceRecordId = result.Data.ServiceRecordId,
                recordNumber = result.Data.RecordNumber
            });
        }

        ShowSuccess($"Servis kaydı oluşturuldu. Kayıt No: {result.Data.RecordNumber}");

        return RedirectToAction(nameof(Create));
    }

    [HttpGet]
    public async Task<IActionResult> GetModels(int brandId)
    {
        var result = await _serviceRecordApiService.GetModelsAsync(brandId);

        if (result.IsFailure || result.Data is null)
            return Json(new List<VehicleModelViewModel>());

        return Json(result.Data);
    }

    [HttpGet("ServiceRecords/Detail/{id:int}")]
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> Detail(int id)
    {
        var result = await _serviceRecordApiService.GetDetailAsync(id);

        if (result.IsFailure || result.Data is null)
        {
            ShowError(result.ErrorMessage ?? "Servis kaydı bulunamadı.");
            return RedirectToAction(nameof(Index));
        }

        var model = result.Data;

        var invoicesResult = await _invoiceApiService.GetListByServiceRecordAsync(id);
        if (invoicesResult.IsSuccess && invoicesResult.Data is not null)
        {
            model.Invoices = invoicesResult.Data;
        }

        var draftInvoiceResult = await _invoiceApiService.GetDraftByServiceRecordAsync(id);
        if (draftInvoiceResult.IsSuccess && draftInvoiceResult.Data is not null)
        {
            model.DraftInvoiceId = draftInvoiceResult.Data.Id;
        }

        var activeInvoiceResult = await _invoiceApiService.GetActiveInvoiceByServiceRecordAsync(id);
        if (activeInvoiceResult.IsSuccess && activeInvoiceResult.Data is not null)
        {
            model.ActiveInvoiceId = activeInvoiceResult.Data.InvoiceId;
            model.ActiveInvoiceStatus = activeInvoiceResult.Data.Status;
            model.ActiveInvoiceNumber = activeInvoiceResult.Data.InvoiceNumber;
        }

        var stockResult = await _serviceRecordApiService.GetStockSelectListAsync();
        if (stockResult.IsSuccess && stockResult.Data is not null)
        {
            model.StockItems = stockResult.Data;
        }

        return View(model);
    }

    [HttpGet("ServiceRecords/SearchCustomers")]
    public async Task<IActionResult> SearchCustomers(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Json(new List<object>());

        var result = await _serviceRecordApiService.SearchCustomersAsync(query);

        if (result.IsFailure || result.Data is null)
            return Json(new List<object>());

        return Json(result.Data);
    }

    [HttpPost("ServiceRecords/AssignQrCode")]
    public async Task<IActionResult> AssignQrCode(int vehicleId, string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return BadRequest("QR kod zorunludur.");

        code = NormalizeQrCode(code);

        var result = await _serviceRecordApiService.AssignQrCodeAsync(vehicleId, code);

        if (result.IsFailure)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("ServiceRecords/UpdateRequestItem")]
    public async Task<IActionResult> UpdateRequestItem(UpdateServiceRequestItemViewModel model)
    {
        var result = await _serviceRecordApiService.UpdateRequestItemAsync(model);

        if (result.IsFailure)
        {
            ShowError(result.ErrorMessage ?? "Talep güncellenirken hata oluştu.");
        }
        else
        {
            ShowSuccess("Talep başarıyla güncellendi.");
        }

        return RedirectToAction(nameof(Detail), new { id = model.ServiceRecordId });
    }

    [HttpPost("ServiceRecords/AddRequestItem")]
    public async Task<IActionResult> AddRequestItem(
        CreateServiceRequestItemViewModel model,
        int serviceRecordId)
    {
        var result = await _serviceRecordApiService.AddRequestItemAsync(model, serviceRecordId);

        if (result.IsFailure)
            return BadRequest(result);

        return Json(new
        {
            id = result.Data,
            title = model.Title,
            note = model.Note,
            estimatedAmount = model.EstimatedAmount
        });
    }

    [HttpPost("ServiceRecords/AddOperation")]
    public async Task<IActionResult> AddOperation(AddServiceOperationViewModel model)
    {
        var result = await _serviceRecordApiService.AddOperationAsync(model);

        if (result.IsFailure || result.Data is null)
            return BadRequest(result);

        return Json(result.Data);
    }

    [HttpPost("ServiceRecords/UpdateStatus")]
    public async Task<IActionResult> UpdateStatus(UpdateServiceRecordStatusViewModel model)
    {
        var result = await _serviceRecordApiService.UpdateStatusAsync(model);

        if (result.IsFailure)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("ServiceRecords/DeleteOperation")]
    public async Task<IActionResult> DeleteOperation(int operationId)
    {
        var result = await _serviceRecordApiService.DeleteOperationAsync(operationId);

        if (result.IsFailure)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("ServiceRecords/DeleteRequestItem")]
    public async Task<IActionResult> DeleteRequestItem(int requestItemId)
    {
        var result = await _serviceRecordApiService.DeleteRequestItemAsync(requestItemId);

        if (result.IsFailure)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpGet("ServiceRecords/SearchStockItems")]
    public async Task<IActionResult> SearchStockItems([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
            return Ok(new List<StockItemSelectViewModel>());

        var result = await _serviceRecordApiService.SearchStockItemsAsync(q.Trim());

        if (result.IsFailure || result.Data is null)
            return Ok(new List<StockItemSelectViewModel>());

        return Ok(result.Data);
    }

    private async Task<List<VehicleBrandViewModel>> GetBrandsAsync()
    {
        var result = await _serviceRecordApiService.GetBrandsAsync();

        return result.Data ?? new List<VehicleBrandViewModel>();
    }

    private bool IsAjaxRequest()
    {
        return Request.Headers["X-Requested-With"] == "XMLHttpRequest";
    }

    private static string NormalizeQrCode(string code)
    {
        code = code.Trim();

        if (Uri.TryCreate(code, UriKind.Absolute, out var uri))
        {
            code = uri.Segments.Last().Trim('/');
        }

        return code;
    }
}