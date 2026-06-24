using AutoStock.Services.Dtos.Invoices;
using AutoStock.Services.Dtos.Vehicles;
using AutoStock.WEB.Models.Invoices;
using AutoStock.WEB.Services;
using Microsoft.AspNetCore.Mvc;

namespace AutoStock.WEB.Controllers;

public class InvoicesController : BaseController
{
    private readonly InvoiceApiService _invoiceApiService;
    private readonly InvoicePageService _invoicePageService;

    public InvoicesController(
        InvoiceApiService invoiceApiService,
        InvoicePageService invoicePageService)
    {
        _invoiceApiService = invoiceApiService;
        _invoicePageService = invoicePageService;
    }

    [HttpGet("Invoices")]
    public async Task<IActionResult> Index([FromQuery] InvoiceListQueryViewModel query)
    {
        var pageResult = await _invoicePageService.BuildIndexAsync(query);

        if (pageResult.HasErrors)
        {
            ShowErrors(pageResult.ErrorMessages);
        }

        return View(pageResult.ViewModel);
    }

    [HttpGet("Invoices/CreateFromServiceRecord/{serviceRecordId:int}")]
    public async Task<IActionResult> CreateFromServiceRecord(int serviceRecordId)
    {
        var result = await _invoicePageService.CreateOrGetDraftFromServiceRecordAsync(serviceRecordId);

        if (result.IsFailure || result.Data is null)
        {
            ShowError(result.ErrorMessage ?? "Fatura hazırlanırken hata oluştu.");

            return RedirectToAction(
                "Detail",
                "ServiceRecords",
                new { id = serviceRecordId });
        }

        return RedirectToAction(
            nameof(Detail),
            new { invoiceId = result.Data.InvoiceId });
    }

    [HttpPost("Invoices/CreateDraftFromServiceRecord/{serviceRecordId:int}")]
    public async Task<IActionResult> CreateDraftFromServiceRecord(int serviceRecordId)
    {
        var result = await _invoicePageService.CreateOrGetDraftFromServiceRecordAsync(serviceRecordId);
        var statusCode = result.StatusCode > 0
            ? result.StatusCode
            : result.IsSuccess ? StatusCodes.Status200OK : StatusCodes.Status400BadRequest;

        return StatusCode(statusCode, result);
    }

    [HttpPost("Invoices/CreateFromServiceRecord")]
    public async Task<IActionResult> CreateFromServiceRecord([FromBody] InvoiceCreateViewModel model)
    {
        if (model.Items is null || !model.Items.Any())
        {
            return BadRequest(new
            {
                isSuccess = false,
                errorMessages = new[] { "Fatura kalemi zorunludur." }
            });
        }

        var result = await _invoiceApiService.CreateAsync(model);

        if (result.IsFailure)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpGet("Invoices/Print/{invoiceId:int}")]
    public async Task<IActionResult> Print(int invoiceId)
    {
        var result = await _invoiceApiService.GetDetailAsync(invoiceId);

        return ViewObjectResult(
            result,
            "Fatura bulunamadı.",
            onFailure: () => RedirectToAction(nameof(Index)));
    }

    [HttpGet("Invoices/Detail/{invoiceId:int}")]
    public async Task<IActionResult> Detail(int invoiceId)
    {
        var pageResult = await _invoicePageService.BuildDetailAsync(invoiceId);

        if (pageResult.HasErrors)
        {
            ShowErrors(pageResult.ErrorMessages);
            return RedirectToAction(nameof(Index));
        }

        return View(pageResult.ViewModel);
    }

    [HttpPost("Invoices/Issue/{invoiceId:int}")]
    public async Task<IActionResult> Issue(int invoiceId)
    {
        var result = await _invoiceApiService.IssueAsync(invoiceId);

        if (result.IsFailure)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("Invoices/Cancel/{id:int}")]
    public async Task<IActionResult> Cancel(int id)
    {
        var ownerAccess = RequireOwnerAccess();
        if (ownerAccess is not null)
            return ownerAccess;

        var result = await _invoiceApiService.CancelAsync(id);

        if (result.IsFailure)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpGet("Invoices/Edit/{invoiceId:int}")]
    public async Task<IActionResult> Edit(int invoiceId)
    {
        var result = await _invoiceApiService.GetDetailAsync(invoiceId);

        if (result.IsFailure || result.Data is null)
        {
            ShowError(result.ErrorMessage ?? "Fatura bulunamadı.");
            return RedirectToAction(nameof(Index));
        }

        if (result.Data.Status != 1)
        {
            ShowError("Sadece taslak faturalar düzenlenebilir.");
            return RedirectToAction(nameof(Detail), new { invoiceId });
        }

        return View(result.Data);
    }

    [HttpPut("Invoices/Edit/{invoiceId:int}")]
    public async Task<IActionResult> Edit(int invoiceId, [FromBody] UpdateInvoiceDto model)
    {
        if (model is null)
        {
            return BadRequest(new
            {
                isSuccess = false,
                errorMessage = "Fatura bilgisi alınamadı."
            });
        }

        model.InvoiceId = invoiceId;

        var result = await _invoiceApiService.UpdateAsync(invoiceId, model);

        if (result.IsFailure)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpGet("Invoices/VehicleBrands")]
    public async Task<IActionResult> GetVehicleBrands()
    {
        var result = await _invoiceApiService.GetVehicleBrandsAsync();

        if (result.IsFailure)
            return BadRequest(result);

        return Ok(result.Data ?? new List<VehicleBrandDto>());
    }

    [HttpGet("Invoices/VehicleModels")]
    public async Task<IActionResult> GetVehicleModels([FromQuery] int brandId)
    {
        var result = await _invoiceApiService.GetVehicleModelsAsync(brandId);

        if (result.IsFailure)
            return BadRequest(result);

        return Ok(result.Data ?? new List<VehicleModelDto>());
    }
    [HttpPost("Invoices/{id:int}/SendEmail")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendEmail(int id, string? toEmail, string? message)
    {
        var result = await _invoiceApiService.SendEmailAsync(id, new SendInvoiceEmailRequestDto
        {
            ToEmail = toEmail,
            Message = message
        });

        return HandleCommandResult(
            result,
            onSuccess: () => RedirectToAction(nameof(Detail), new { id }),
            onFailure: () => RedirectToAction(nameof(Detail), new { id }),
            defaultErrorMessage: "Servis hesap özeti e-postası gönderilirken hata oluştu.",
            successMessage: "Servis hesap özeti e-postası gönderildi.");
    }
}
