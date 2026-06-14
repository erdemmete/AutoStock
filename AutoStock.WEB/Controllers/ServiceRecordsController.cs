using AutoStock.Repositories.Enums;
using AutoStock.Services.Dtos.Customers;
using AutoStock.Services.Dtos.ServiceRecords;
using AutoStock.Services.Dtos.Vehicles;
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
        var model = new CreateServiceRecordViewModel();

        await PrepareCreateModelAsync(model);

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateServiceRecordViewModel model)
    {
        await PrepareCreateModelAsync(model);

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

    [HttpGet]
    public async Task<IActionResult> SearchCustomers(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Json(new List<CustomerSearchDto>());

        var result = await _serviceRecordApiService.SearchCustomersAsync(query);

        if (result.IsFailure || result.Data is null)
            return Json(new List<CustomerSearchDto>());

        return Json(result.Data);
    }

    [HttpGet]
    public async Task<IActionResult> SearchVehicles(string plate)
    {
        if (string.IsNullOrWhiteSpace(plate))
            return Json(new List<VehicleSearchDto>());

        var result = await _serviceRecordApiService.SearchVehiclesAsync(plate);

        if (result.IsFailure || result.Data is null)
            return Json(new List<VehicleSearchDto>());

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

    [HttpPost("ServiceRecords/RestoreRequestItem")]
    public async Task<IActionResult> RestoreRequestItem(int requestItemId)
    {
        var result = await _serviceRecordApiService.RestoreRequestItemAsync(requestItemId);

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

    [HttpPost("ServiceRecords/UpdateRequestItem")]
    public async Task<IActionResult> UpdateRequestItem(UpdateServiceRequestItemFormModel form)
    {
        var result = await _serviceRecordPageService.UpdateRequestItemAsync(form);

        if (result.IsFailure)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("ServiceRecords/UpdateOperation")]
    public async Task<IActionResult> UpdateOperation(UpdateServiceOperationFormModel form)
    {
        var result = await _serviceRecordPageService.UpdateOperationAsync(form);

        if (result.IsFailure)
            return BadRequest(result);

        return Ok(result);
    }


    [HttpPost("ServiceRecords/{serviceRecordId:int}/Photos")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadPhoto(int serviceRecordId, List<IFormFile>? files, IFormFile? file, ServiceImageType type, string? description)
    {
        var isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

        var uploadFiles = new List<IFormFile>();

        if (files is not null && files.Any())
        {
            uploadFiles.AddRange(files.Where(x => x is not null && x.Length > 0));
        }

        if (file is not null && file.Length > 0)
        {
            uploadFiles.Add(file);
        }

        if (!uploadFiles.Any())
        {
            if (isAjax)
            {
                return Json(new
                {
                    success = false,
                    message = "En az bir fotoğraf seçmelisin."
                });
            }

            ShowError("En az bir fotoğraf seçmelisin.");
            return RedirectToAction(nameof(Detail), new { id = serviceRecordId });
        }

        var uploadedImages = new List<object>();
        var failedMessages = new List<string>();

        foreach (var uploadFile in uploadFiles)
        {
            var result = await _serviceRecordApiService.UploadImageAsync(
                serviceRecordId,
                uploadFile,
                type,
                description);

            if (result.IsFailure || result.Data is null)
            {
                failedMessages.Add(result.ErrorMessage ?? $"{uploadFile.FileName} yüklenemedi.");
                continue;
            }

            uploadedImages.Add(new
            {
                id = result.Data.Id,
                type = result.Data.Type.ToString(),
                typeText = result.Data.TypeText,
                description = result.Data.Description,
                createdAt = result.Data.CreatedAt,
                imageUrl = Url.Action(nameof(Photo), "ServiceRecords", new { id = result.Data.Id })
            });
        }

        if (!uploadedImages.Any())
        {
            var message = failedMessages.FirstOrDefault() ?? "Fotoğraflar yüklenemedi.";

            if (isAjax)
            {
                return Json(new
                {
                    success = false,
                    message
                });
            }

            ShowError(message);
            return RedirectToAction(nameof(Detail), new { id = serviceRecordId });
        }

        if (isAjax)
        {
            return Json(new
            {
                success = true,
                message = failedMessages.Any()
                    ? $"{uploadedImages.Count} fotoğraf yüklendi. {failedMessages.Count} fotoğraf yüklenemedi."
                    : $"{uploadedImages.Count} fotoğraf başarıyla eklendi.",
                images = uploadedImages
            });
        }

        ShowSuccess($"{uploadedImages.Count} fotoğraf başarıyla eklendi.");
        return RedirectToAction(nameof(Detail), new { id = serviceRecordId });
    }

    [HttpGet("ServiceRecords/Photos/{id:int}")]
    public async Task<IActionResult> Photo(int id)
    {
        var result = await _serviceRecordApiService.GetImageContentAsync(id);

        if (!result.Success)
            return NotFound();

        return File(result.Content, result.ContentType);
    }

    [HttpPost("ServiceRecords/Photos/{id:int}/Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePhoto(int id)
    {
        var result = await _serviceRecordApiService.DeleteImageAsync(id);

        if (result.IsFailure)
        {
            return Json(new
            {
                success = false,
                message = result.ErrorMessage ?? "Fotoğraf silinemedi."
            });
        }

        return Json(new
        {
            success = true,
            message = "Fotoğraf silindi."
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetVariants(int modelId)
    {
        var result = await _serviceRecordApiService.GetVariantsAsync(modelId);

        if (result.IsFailure || result.Data is null)
            return Json(new List<VehicleVariantViewModel>());

        return Json(result.Data);
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

    private async Task PrepareCreateModelAsync(CreateServiceRecordViewModel model)
    {
        model.Brands = await GetBrandsAsync();
        model.ServiceAdvisorName = HttpContext.Session.GetString("FullName") ?? "Oturum Bilgisi Yok";

        var workshopInfoResult = await _serviceRecordApiService.GetCreateWorkshopInfoAsync();

        if (workshopInfoResult.IsSuccess && workshopInfoResult.Data is not null)
        {
            model.WorkshopDisplayName = workshopInfoResult.Data.DisplayName;
            model.WorkshopAddressText = workshopInfoResult.Data.AddressText;
            model.WorkshopPhoneText = workshopInfoResult.Data.PhoneText;
        }
    }
}