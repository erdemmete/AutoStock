using AutoStock.Services.Constants;
using AutoStock.Services.Dtos.Invoices;
using AutoStock.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoStock.API.Controllers;

[ApiController]
[Route("api/invoices/export")]
[Authorize(Roles = AppRoles.Owner)]
public class InvoiceExportsController : BaseApiController
{
    private readonly IInvoiceExportService _invoiceExportService;

    public InvoiceExportsController(IInvoiceExportService invoiceExportService)
    {
        _invoiceExportService = invoiceExportService;
    }

    [HttpGet("preview")]
    public async Task<IActionResult> Preview([FromQuery] InvoiceExportQueryDto query)
    {
        var workshopIdResult = GetCurrentWorkshopId();

        if (workshopIdResult.IsFailure)
            return UnauthorizedResult(workshopIdResult);

        var result = await _invoiceExportService.GetPreviewAsync(
            query,
            workshopIdResult.Data);

        return ToActionResult(result);
    }

    [HttpGet("download")]
    public async Task<IActionResult> Download([FromQuery] InvoiceExportQueryDto query)
    {
        var workshopIdResult = GetCurrentWorkshopId();

        if (workshopIdResult.IsFailure)
            return UnauthorizedResult(workshopIdResult);

        var result = await _invoiceExportService.CreateZipAsync(
            query,
            workshopIdResult.Data);

        if (result.IsFailure || result.Data is null)
            return ToActionResult(result);

        return File(
            result.Data.Content,
            result.Data.ContentType,
            result.Data.FileName);
    }

    [HttpPost("send-email")]
    public async Task<IActionResult> SendEmail(SendInvoiceExportEmailRequestDto request)
    {
        var workshopIdResult = GetCurrentWorkshopId();

        if (workshopIdResult.IsFailure)
            return UnauthorizedResult(workshopIdResult);

        var result = await _invoiceExportService.SendEmailAsync(
            request,
            workshopIdResult.Data);

        return ToActionResult(result);
    }
}
