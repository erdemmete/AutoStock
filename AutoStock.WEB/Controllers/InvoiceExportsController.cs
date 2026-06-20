using AutoStock.WEB.Models.Invoices;
using AutoStock.WEB.Services;
using Microsoft.AspNetCore.Mvc;

namespace AutoStock.WEB.Controllers;

public class InvoiceExportsController : BaseController
{
    private readonly InvoiceExportApiService _invoiceExportApiService;
    private readonly InvoiceExportPageService _invoiceExportPageService;
    private readonly AccountingInvoiceRequestApiService _accountingInvoiceRequestApiService;

    public InvoiceExportsController(
        InvoiceExportApiService invoiceExportApiService,
        InvoiceExportPageService invoiceExportPageService,
        AccountingInvoiceRequestApiService accountingInvoiceRequestApiService)
    {
        _invoiceExportApiService = invoiceExportApiService;
        _invoiceExportPageService = invoiceExportPageService;
        _accountingInvoiceRequestApiService = accountingInvoiceRequestApiService;
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
                : new[] { result.ErrorMessage ?? "Belgeler indirilemedi." });

            return RedirectToAction(nameof(Index), new
            {
                query.StartDate,
                query.EndDate,
                query.Preset,
                query.Tab,
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
            ShowError("Faturayı hazırlayacak kişinin e-posta adresi zorunludur.");

            return RedirectToAction(nameof(Index), new
            {
                query.StartDate,
                query.EndDate,
                query.Preset,
                query.Tab,
                query.IncludeCancelled
            });
        }

        if (query.InvoiceIds is null || !query.InvoiceIds.Any())
        {
            ShowError("En az bir servis hesap özeti seçiniz.");

            return RedirectToAction(nameof(Index), new
            {
                query.StartDate,
                query.EndDate,
                query.Preset,
                query.Tab,
                query.IncludeCancelled
            });
        }

        var result = await _accountingInvoiceRequestApiService.SendBatchAsync(new AutoStock.WEB.Models.Accounting.SendAccountingInvoiceBatchRequestViewModel
        {
            InvoiceIds = query.InvoiceIds,
            RecipientEmail = query.ToEmail,
            Message = query.Message,
            PublicBaseUrl = $"{Request.Scheme}://{Request.Host}"
        });

        if (result.IsSuccess)
        {
            var sentCount = result.Data?.SentCount ?? 0;
            ShowSuccess(sentCount == 1
                ? "1 servis hesap özeti fatura hazırlığına gönderildi."
                : $"{sentCount} servis hesap özeti fatura hazırlığına gönderildi.");
        }
        else
        {
            ShowErrors(result.ErrorMessages.Any()
                ? result.ErrorMessages
                : new[] { result.ErrorMessage ?? "Fatura hazırlığına gönderilirken hata oluştu." });
        }

        return RedirectToAction(nameof(Index), new
        {
            query.StartDate,
            query.EndDate,
            query.Preset,
            query.Tab,
            query.IncludeCancelled
        });
    }
}
