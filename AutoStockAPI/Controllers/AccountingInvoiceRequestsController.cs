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

            var safeReturnUrl = ResolveSafeReturnUrl(returnUrl, token, Request);

            if (result.IsFailure)
            {
                return Redirect(AddQueryParameter(
                    safeReturnUrl,
                    "uploadError",
                    result.ErrorMessage ?? "Yükleme başarısız."));
            }

            return Redirect(AddQueryParameter(safeReturnUrl, "uploaded", "1"));
        }

        private static string ResolveSafeReturnUrl(
            string? returnUrl,
            string token,
            HttpRequest request)
        {
            var fallback = $"/Accounting/InvoiceRequest/{Uri.EscapeDataString(token)}";

            if (string.IsNullOrWhiteSpace(returnUrl))
                return fallback;

            var trimmedReturnUrl = returnUrl.Trim();

            if (IsSafeLocalAccountingReturnUrl(trimmedReturnUrl))
                return trimmedReturnUrl;

            if (!Uri.TryCreate(trimmedReturnUrl, UriKind.Absolute, out var uri))
                return fallback;

            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
                return fallback;

            if (!string.Equals(uri.Host, request.Host.Host, StringComparison.OrdinalIgnoreCase))
                return fallback;

            if (!IsAccountingInvoiceRequestPath(uri.AbsolutePath))
                return fallback;

            return uri.ToString();
        }

        private static bool IsSafeLocalAccountingReturnUrl(string returnUrl)
        {
            if (!returnUrl.StartsWith("/", StringComparison.Ordinal) ||
                returnUrl.StartsWith("//", StringComparison.Ordinal) ||
                returnUrl.StartsWith("/\\", StringComparison.Ordinal) ||
                returnUrl.Contains('\\'))
            {
                return false;
            }

            return IsAccountingInvoiceRequestPath(returnUrl);
        }

        private static bool IsAccountingInvoiceRequestPath(string path)
        {
            return path.Contains(
                "/Accounting/InvoiceRequest/",
                StringComparison.OrdinalIgnoreCase);
        }

        private static string AddQueryParameter(string url, string key, string value)
        {
            var separator = url.Contains('?') ? '&' : '?';
            return $"{url}{separator}{Uri.EscapeDataString(key)}={Uri.EscapeDataString(value)}";
        }
    }
}
