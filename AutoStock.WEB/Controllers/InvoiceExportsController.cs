using AutoStock.WEB.Models.Invoices;
using AutoStock.WEB.Services;
using Microsoft.AspNetCore.Mvc;

namespace AutoStock.WEB.Controllers;

public class InvoiceExportsController : BaseController
{
    private readonly InvoiceExportApiService _invoiceExportApiService;
    private readonly InvoiceExportPageService _invoiceExportPageService;

    public InvoiceExportsController(
        InvoiceExportApiService invoiceExportApiService,
        InvoiceExportPageService invoiceExportPageService)
    {
        _invoiceExportApiService = invoiceExportApiService;
        _invoiceExportPageService = invoiceExportPageService;
    }

    [HttpGet("Invoices/Export")]
    public async Task<IActionResult> Index([FromQuery] InvoiceExportQueryViewModel query)
    {
        var ownerAccess = RequireOwnerAccess();
        if (ownerAccess is not null)
            return ownerAccess;

        var pageResult = await _invoiceExportPageService.BuildIndexAsync(query);

        if (pageResult.HasErrors)
        {
            ShowErrors(pageResult.ErrorMessages);
        }

        return View(pageResult.ViewModel);
    }

    [HttpGet("Invoices/Export/Download")]
    public async Task<IActionResult> Download([FromQuery] InvoiceExportQueryViewModel query)
    {
        var ownerAccess = RequireOwnerAccess();
        if (ownerAccess is not null)
            return ownerAccess;

        var result = await _invoiceExportApiService.DownloadAsync(query);

        if (!result.IsSuccess)
        {
            ShowErrors(result.ErrorMessages.Any()
                ? result.ErrorMessages
                : new[] { result.ErrorMessage ?? "Fatura aktarım paketi indirilemedi." });

            return RedirectToAction(nameof(Index), new
            {
                query.StartDate,
                query.EndDate,
                query.Preset,
                query.IncludeCancelled
            });
        }

        return File(result.Content, result.ContentType, result.FileName);
    }

    [HttpPost("Invoices/Export/SendEmail")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendEmail(InvoiceExportQueryViewModel query)
    {
        var ownerAccess = RequireOwnerAccess();
        if (ownerAccess is not null)
            return ownerAccess;

        if (string.IsNullOrWhiteSpace(query.ToEmail))
        {
            ShowError("Muhasebeci e-posta adresi zorunludur.");

            return RedirectToAction(nameof(Index), new
            {
                query.StartDate,
                query.EndDate,
                query.Preset,
                query.IncludeCancelled
            });
        }

        var result = await _invoiceExportApiService.SendEmailAsync(query);

        return HandleCommandResult(
            result,
            onSuccess: () => RedirectToAction(nameof(Index), new
            {
                query.StartDate,
                query.EndDate,
                query.Preset,
                query.IncludeCancelled
            }),
            onFailure: () => RedirectToAction(nameof(Index), new
            {
                query.StartDate,
                query.EndDate,
                query.Preset,
                query.IncludeCancelled
            }),
            defaultErrorMessage: "Fatura aktarım e-postası gönderilirken hata oluştu.",
            successMessage: "Fatura aktarım paketi e-posta ile gönderildi.");
    }
}
