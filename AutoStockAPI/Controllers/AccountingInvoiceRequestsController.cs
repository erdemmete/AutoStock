using AutoStock.Services.Constants;
using AutoStock.Services.Dtos.Accounting;
using AutoStock.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoStock.API.Controllers
{
    [ApiController]
    [Route("api/accounting-invoice-requests")]
    public class AccountingInvoiceRequestsController : BaseApiController
    {
        private readonly IAccountingInvoiceRequestService _accountingInvoiceRequestService;

        public AccountingInvoiceRequestsController(IAccountingInvoiceRequestService accountingInvoiceRequestService)
        {
            _accountingInvoiceRequestService = accountingInvoiceRequestService;
        }

        [Authorize(Roles = AppRoles.Owner)]
        [HttpGet("recipients")]
        public async Task<IActionResult> GetRecipients()
        {
            var workshopIdResult = GetCurrentWorkshopId();

            if (workshopIdResult.IsFailure)
                return UnauthorizedResult(workshopIdResult);

            var result = await _accountingInvoiceRequestService.GetAccountingRecipientsAsync(workshopIdResult.Data);

            return ToActionResult(result);
        }

        [Authorize(Roles = AppRoles.Owner)]
        [HttpPost("recipients")]
        public async Task<IActionResult> SaveRecipient(CreateAccountingEmailRecipientDto request)
        {
            var workshopIdResult = GetCurrentWorkshopId();

            if (workshopIdResult.IsFailure)
                return UnauthorizedResult(workshopIdResult);

            var result = await _accountingInvoiceRequestService.SaveAccountingRecipientAsync(
                request,
                workshopIdResult.Data);

            return ToActionResult(result);
        }

        [Authorize(Roles = AppRoles.Owner)]
        [HttpPost("send")]
        public async Task<IActionResult> Send(SendAccountingInvoiceRequestDto request)
        {
            var workshopIdResult = GetCurrentWorkshopId();

            if (workshopIdResult.IsFailure)
                return UnauthorizedResult(workshopIdResult);

            var result = await _accountingInvoiceRequestService.SendAccountingRequestAsync(
                request,
                workshopIdResult.Data);

            return ToActionResult(result);
        }

        [Authorize(Roles = AppRoles.Owner + "," + AppRoles.Staff)]
        [HttpGet("invoices/{invoiceId:int}/status")]
        public async Task<IActionResult> GetInvoiceStatus(int invoiceId)
        {
            var workshopIdResult = GetCurrentWorkshopId();

            if (workshopIdResult.IsFailure)
                return UnauthorizedResult(workshopIdResult);

            var result = await _accountingInvoiceRequestService.GetInvoiceAccountingStatusAsync(
                invoiceId,
                workshopIdResult.Data);

            return ToActionResult(result);
        }

        [Authorize(Roles = AppRoles.Owner + "," + AppRoles.Staff)]
        [HttpGet("official-documents/{documentId:int}/download")]
        public async Task<IActionResult> DownloadOfficialInvoice(int documentId)
        {
            var workshopIdResult = GetCurrentWorkshopId();

            if (workshopIdResult.IsFailure)
                return UnauthorizedResult(workshopIdResult);

            var result = await _accountingInvoiceRequestService.GetOfficialInvoiceFileAsync(
                documentId,
                workshopIdResult.Data);

            if (result.IsFailure || result.Data is null)
                return ToActionResult(result);

            return File(
                result.Data.Content,
                result.Data.ContentType,
                result.Data.FileName);
        }

        [AllowAnonymous]
        [HttpGet("public/{token}")]
        public async Task<IActionResult> GetPublicRequest(string token)
        {
            var result = await _accountingInvoiceRequestService.GetPublicRequestAsync(token);

            return ToActionResult(result);
        }

        [AllowAnonymous]
        [HttpPost("public/{token}/upload")]
        [RequestSizeLimit(12 * 1024 * 1024)]
        public async Task<IActionResult> UploadOfficialInvoice(
            string token,
            [FromForm] string officialInvoiceNumber,
            [FromForm] DateTime officialInvoiceDate,
            [FromForm] string? ettnOrUuid,
            [FromForm] string uploadedByEmail,
            [FromForm] string? note,
            [FromForm] string? returnUrl,
            [FromForm] IFormFile file)
        {
            if (file is null)
                return BadRequest("PDF dosyası seçiniz.");

            await using var stream = file.OpenReadStream();

            var result = await _accountingInvoiceRequestService.UploadOfficialInvoiceAsync(
                token,
                new UploadOfficialInvoiceDto
                {
                    OfficialInvoiceNumber = officialInvoiceNumber,
                    OfficialInvoiceDate = officialInvoiceDate,
                    EttnOrUuid = ettnOrUuid,
                    UploadedByEmail = uploadedByEmail,
                    Note = note,
                    FileName = file.FileName,
                    ContentType = file.ContentType,
                    FileSizeBytes = file.Length,
                    FileContent = stream
                });

            var safeReturnUrl = ResolveSafeReturnUrl(returnUrl);

            if (result.IsFailure)
            {
                if (!string.IsNullOrWhiteSpace(safeReturnUrl))
                    return Redirect($"{safeReturnUrl}{(safeReturnUrl.Contains('?') ? '&' : '?')}uploadError={Uri.EscapeDataString(result.ErrorMessage ?? "Yükleme başarısız.")}");

                return BadRequest(result.ErrorMessage ?? "Yükleme başarısız.");
            }

            if (!string.IsNullOrWhiteSpace(safeReturnUrl))
                return Redirect($"{safeReturnUrl}{(safeReturnUrl.Contains('?') ? '&' : '?')}uploaded=1");

            return Ok(result.Data);
        }

        private static string? ResolveSafeReturnUrl(string? returnUrl)
        {
            if (string.IsNullOrWhiteSpace(returnUrl))
                return null;

            if (!Uri.TryCreate(returnUrl, UriKind.Absolute, out var uri))
                return null;

            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
                return null;

            if (!uri.AbsolutePath.Contains("/Accounting/InvoiceRequest/", StringComparison.OrdinalIgnoreCase))
                return null;

            return returnUrl;
        }
    }
}
