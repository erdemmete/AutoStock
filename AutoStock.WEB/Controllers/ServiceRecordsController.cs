using AutoStock.Repositories.Enums;
using AutoStock.Services.Dtos.Customers;
using AutoStock.Services.Dtos.ServiceRecords;
using AutoStock.Services.Dtos.Vehicles;
using AutoStock.Web.Models.ServiceRecords;
using AutoStock.WEB.Controllers;
using AutoStock.WEB.Helpers;
using AutoStock.WEB.Models.Invoices;
using AutoStock.WEB.Models.ServiceRecords;
using AutoStock.WEB.Models.SupportRequests;
using AutoStock.WEB.Models.StockItems;
using AutoStock.WEB.Services;
using Microsoft.AspNetCore.Mvc;
using AutoStock.Services.Calculations;

namespace AutoStock.Web.Controllers;

public class ServiceRecordsController : BaseController
{
    private readonly ServiceRecordApiService _serviceRecordApiService;
    private readonly ServiceRecordPageService _serviceRecordPageService;
    private readonly InvoiceApiService _invoiceApiService;
    private readonly SupportRequestApiService _supportRequestApiService;

    public ServiceRecordsController(
        ServiceRecordApiService serviceRecordApiService,
        ServiceRecordPageService serviceRecordPageService,
        InvoiceApiService invoiceApiService,
        SupportRequestApiService supportRequestApiService)
    {
        _serviceRecordApiService = serviceRecordApiService;
        _serviceRecordPageService = serviceRecordPageService;
        _invoiceApiService = invoiceApiService;
        _supportRequestApiService = supportRequestApiService;
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

    [HttpPost("ServiceRecords/VehicleCatalogSupportRequest")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateVehicleCatalogSupportRequest([FromBody] CreateVehicleCatalogSupportRequestViewModel model)
    {
        const string successMessage = "Araç katalog talebiniz Sente360 Destek ekibine iletildi. Servis kaydına devam edebilirsiniz.";
        const string failureMessage = "Talep oluşturulurken hata oluştu. Servis kaydına devam edebilirsiniz, daha sonra Destek ekranından talep açabilirsiniz.";

        if (!IsOwner && !IsStaff)
        {
            return Unauthorized(new
            {
                success = false,
                message = failureMessage
            });
        }

        var missingVehicleInfo = NormalizeSupportRequestText(model?.MissingVehicleInfo);

        if (string.IsNullOrWhiteSpace(missingVehicleInfo))
        {
            return BadRequest(new
            {
                success = false,
                message = "Lütfen eklenmesini istediğiniz araç bilgisini yazın."
            });
        }

        var supportRequest = new CreateIssueSupportRequestViewModel
        {
            Subject = "Araç katalog ekleme talebi",
            Description = BuildVehicleCatalogSupportDescription(model!, missingVehicleInfo),
            Priority = SupportRequestPriority.Normal
        };

        var result = await _supportRequestApiService.CreateIssueAsync(supportRequest);

        if (result.IsFailure || result.Data <= 0)
        {
            return BadRequest(new
            {
                success = false,
                message = failureMessage
            });
        }

        return Json(new
        {
            success = true,
            message = successMessage,
            supportRequestId = result.Data,
            detailUrl = Url.Action("Detail", "SupportRequests", new { id = result.Data }) ?? $"/SupportRequests/Detail/{result.Data}"
        });
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

    [HttpGet("ServiceRecords/GetVehiclePrefill")]
    public async Task<IActionResult> GetVehiclePrefill([FromQuery] int vehicleId)
    {
        if (vehicleId <= 0)
        {
            return BadRequest(new
            {
                message = "Araç bilgisi geçersiz."
            });
        }

        var result = await _serviceRecordApiService.GetVehiclePrefillAsync(vehicleId);

        if (result.IsFailure || result.Data is null)
        {
            return BadRequest(new
            {
                message = result.ErrorMessage ?? "Araç bilgisi alınamadı.",
                errorMessages = result.ErrorMessages
            });
        }

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
            return BadRequest(new
            {
                success = false,
                message = result.ErrorMessages.FirstOrDefault()
                    ?? result.ErrorMessage
                    ?? "QR kod araca bağlanamadı."
            });

        return Ok(new
        {
            success = true,
            message = "QR kod araca bağlandı."
        });
    }

    [HttpPost("ServiceRecords/CreateVehicleQrCode")]
    public async Task<IActionResult> CreateVehicleQrCode(int vehicleId)
    {
        if (vehicleId <= 0)
        {
            return BadRequest(new
            {
                success = false,
                message = "Araç bilgisi geçersiz."
            });
        }

        var result = await _serviceRecordApiService.CreateVehicleQrCodeAsync(vehicleId);

        if (result.IsFailure || result.Data is null)
        {
            return BadRequest(new
            {
                success = false,
                message = result.ErrorMessages.FirstOrDefault()
                    ?? result.ErrorMessage
                    ?? "Araç QR'ı oluşturulamadı."
            });
        }

        return Json(new
        {
            success = true,
            message = result.Data.ReplacedExistingQr
                ? "Araç QR'ı değiştirildi."
                : "Araç QR'ı oluşturuldu.",
            code = result.Data.Code,
            vehicleId = result.Data.VehicleId,
            replacedExistingQr = result.Data.ReplacedExistingQr
        });
    }

    [HttpPost("ServiceRecords/EnsureVehicleQrCode")]
    public async Task<IActionResult> EnsureVehicleQrCode(int vehicleId)
    {
        if (vehicleId <= 0)
        {
            return BadRequest(new
            {
                success = false,
                message = "Araç bilgisi geçersiz."
            });
        }

        var result = await _serviceRecordApiService.EnsureVehicleQrCodeAsync(vehicleId);

        if (result.IsFailure || result.Data is null)
        {
            return BadRequest(new
            {
                success = false,
                message = result.ErrorMessages.FirstOrDefault()
                    ?? result.ErrorMessage
                    ?? "Güvenli belge bağlantısı hazırlanamadı."
            });
        }

        return Json(new
        {
            success = true,
            code = result.Data.Code,
            vehicleId = result.Data.VehicleId
        });
    }

    [HttpGet("ServiceRecords/VehicleQr/{vehicleId:int}/Download")]
    public async Task<IActionResult> DownloadVehicleQr(int vehicleId)
    {
        var publicBaseUrl = $"{Request.Scheme}://{Request.Host}";
        var result = await _serviceRecordApiService.DownloadVehicleQrPngAsync(vehicleId, publicBaseUrl);

        if (!result.Success)
        {
            ShowError(result.ErrorMessage ?? "QR görseli indirilemedi.");
            var referer = Request.Headers.Referer.ToString();

            if (!string.IsNullOrWhiteSpace(referer) &&
                Url.IsLocalUrl(new Uri(referer, UriKind.RelativeOrAbsolute).IsAbsoluteUri
                    ? new Uri(referer).PathAndQuery
                    : referer))
            {
                var localReferer = new Uri(referer, UriKind.RelativeOrAbsolute).IsAbsoluteUri
                    ? new Uri(referer).PathAndQuery
                    : referer;

                return Redirect(localReferer);
            }

            return RedirectToAction(nameof(Index));
        }

        return File(result.Content, result.ContentType, $"arac-{vehicleId}-qr.png");
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

        return Json(await BuildOperationMutationPayloadAsync(
            model.ServiceRecordId,
            result.Data,
            model.ServiceRequestItemId));
    }

    [HttpPost("ServiceRecords/UpdateStatus")]
    public async Task<IActionResult> UpdateStatus(UpdateServiceRecordStatusViewModel model)
    {
        if (model.Status == (int)ServiceRecordStatus.Cancelled)
        {
            var ownerAccess = RequireOwnerAccess();
            if (ownerAccess is not null)
                return ownerAccess;
        }

        var result = await _serviceRecordApiService.UpdateStatusAsync(model);

        if (result.IsFailure)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("ServiceRecords/DeleteOperation")]
    public async Task<IActionResult> DeleteOperation(int operationId)
    {
        var result = await _serviceRecordApiService.DeleteOperationAsync(operationId);

        if (result.IsFailure || result.Data is null)
            return BadRequest(result);

        return Json(await BuildOperationMutationPayloadAsync(
            result.Data.ServiceRecordId,
            operation: null,
            result.Data.ServiceRequestItemId,
            result.Data.RecordTotal,
            result.Data.RequestItemTotal));
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

        if (result.IsFailure || result.Data is null)
            return BadRequest(result);

        return Json(await BuildOperationMutationPayloadAsync(
            form.ServiceRecordId,
            result.Data,
            result.Data.ServiceRequestItemId ?? form.ServiceRequestItemId));
    }

    private async Task<object> BuildOperationMutationPayloadAsync(
        int serviceRecordId,
        ServiceOperationDto? operation,
        int? serviceRequestItemId,
        decimal? fallbackRecordTotal = null,
        decimal? fallbackRequestTotal = null)
    {
        var detailResult = await _serviceRecordApiService.GetDetailAsync(serviceRecordId);
        var operations = detailResult.IsSuccess && detailResult.Data is not null
            ? detailResult.Data.Operations
            : new List<ServiceOperationViewModel>();

        var requestOperations = operations
            .Where(x => x.ServiceRequestItemId == serviceRequestItemId)
            .ToList();

        var serviceTotal = detailResult.Data?.TotalAmount
            ?? fallbackRecordTotal
            ?? operation?.TotalPrice
            ?? 0m;

        var requestTotal = detailResult.Data is not null
            ? requestOperations.Sum(x => x.TotalPrice)
            : fallbackRequestTotal ?? 0m;

        var totals = ServiceRecordTotalsCalculator.CalculateServiceOperations(
            new[] { (1m, serviceTotal) });
        var vatTotal = totals.Vat;
        var grandTotal = totals.GrandTotal;

        return new
        {
            operation = operation is null
                ? null
                : new
                {
                    id = operation.Id,
                    type = (int)operation.Type,
                    typeText = (int)operation.Type == 1 ? "Parça" : "İşçilik",
                    typeClass = (int)operation.Type == 1 ? "part" : "labor",
                    description = operation.Description,
                    quantity = operation.Quantity,
                    unitPrice = operation.UnitPrice,
                    totalPrice = operation.TotalPrice,
                    note = operation.Note,
                    serviceRequestItemId = operation.ServiceRequestItemId
                },
            request = new
            {
                id = serviceRequestItemId,
                operationCount = detailResult.Data is not null
                    ? requestOperations.Count
                    : (int?)null,
                operationTotal = requestTotal,
                operationTotalText = SenteMoney.Format(requestTotal)
            },
            summary = new
            {
                subTotal = serviceTotal,
                vatTotal,
                grandTotal,
                subTotalText = SenteMoney.Format(serviceTotal),
                vatTotalText = SenteMoney.Format(vatTotal),
                grandTotalText = SenteMoney.Format(grandTotal)
            }
        };
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

    private static string BuildVehicleCatalogSupportDescription(
        CreateVehicleCatalogSupportRequestViewModel model,
        string missingVehicleInfo)
    {
        var lines = new List<string>
        {
            "Eksik araç bilgisi:",
            missingVehicleInfo,
            string.Empty,
            "Servis kaydı oluşturma ekranındaki mevcut bağlam:"
        };

        AddDescriptionLine(lines, "Seçili marka", model.SelectedBrandText);
        AddDescriptionLine(lines, "Seçili model", model.SelectedModelText);
        AddDescriptionLine(lines, "Seçili versiyon", model.SelectedVariantText);
        AddDescriptionLine(lines, "Plaka", model.Plate);
        AddDescriptionLine(lines, "Model yılı", model.ModelYear);
        AddDescriptionLine(lines, "Şasi no", model.ChassisNumber);

        lines.Add(string.Empty);
        lines.Add("Bu talep servis kaydı oluşturma ekranındaki araç kataloğu yardım modalından gönderildi.");

        return string.Join(Environment.NewLine, lines);
    }

    private static void AddDescriptionLine(List<string> lines, string label, string? value)
    {
        var normalizedValue = NormalizeSupportRequestText(value);

        if (!string.IsNullOrWhiteSpace(normalizedValue))
        {
            lines.Add($"{label}: {normalizedValue}");
        }
    }

    private static string? NormalizeSupportRequestText(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
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
