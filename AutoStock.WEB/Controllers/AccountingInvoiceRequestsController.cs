using AutoStock.Services.Dtos.Accounting;
using AutoStock.WEB.Models.Accounting;
using AutoStock.WEB.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoStock.WEB.Controllers
{
    public class AccountingInvoiceRequestsController : BaseController
    {
        private readonly AccountingInvoiceRequestApiService _accountingInvoiceRequestApiService;
        private readonly IConfiguration _configuration;

        public AccountingInvoiceRequestsController(
            AccountingInvoiceRequestApiService accountingInvoiceRequestApiService,
            IConfiguration configuration)
        {
            _accountingInvoiceRequestApiService = accountingInvoiceRequestApiService;
            _configuration = configuration;
        }

        [HttpGet("AccountingInvoiceRequests/Recipients")]
        public async Task<IActionResult> Recipients()
        {
            var ownerAccess = RequireOwnerAccess();
            if (ownerAccess is not null)
                return ownerAccess;

            var result = await _accountingInvoiceRequestApiService.GetRecipientsAsync();

            if (result.IsFailure)
                return BadRequest(result);

            return Ok(result.Data ?? new List<AccountingEmailRecipientDto>());
        }

        [HttpPost("AccountingInvoiceRequests/Recipients")]
        public async Task<IActionResult> SaveRecipient([FromBody] CreateAccountingEmailRecipientDto model)
        {
            var ownerAccess = RequireOwnerAccess();
            if (ownerAccess is not null)
                return ownerAccess;

            var result = await _accountingInvoiceRequestApiService.SaveRecipientAsync(model);

            if (result.IsFailure)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPost("AccountingInvoiceRequests/Send")]
        public async Task<IActionResult> Send([FromBody] SendAccountingInvoiceRequestViewModel model)
        {
            var ownerAccess = RequireOwnerAccess();
            if (ownerAccess is not null)
                return ownerAccess;

            if (model is null || model.InvoiceId <= 0)
            {
                return BadRequest(new
                {
                    isSuccess = false,
                    errorMessage = "Hesap özeti bilgisi alınamadı."
                });
            }

            model.PublicBaseUrl = BuildPublicBaseUrl();

            var result = await _accountingInvoiceRequestApiService.SendAsync(model);

            if (result.IsFailure)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPost("AccountingInvoiceRequests/SendBatch")]
        public async Task<IActionResult> SendBatch([FromBody] SendAccountingInvoiceBatchRequestViewModel model)
        {
            var ownerAccess = RequireOwnerAccess();
            if (ownerAccess is not null)
                return ownerAccess;

            if (model is null || model.InvoiceIds is null || !model.InvoiceIds.Any())
            {
                return BadRequest(new
                {
                    isSuccess = false,
                    errorMessage = "En az bir servis hesap özeti seçiniz."
                });
            }

            model.PublicBaseUrl = BuildPublicBaseUrl();

            var result = await _accountingInvoiceRequestApiService.SendBatchAsync(model);

            if (result.IsFailure)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPost("AccountingInvoiceRequests/OfficialDocument/{documentId:int}/MarkDelivered")]
        public async Task<IActionResult> MarkDelivered(int documentId, [FromBody] MarkOfficialInvoiceDeliveredDto model)
        {
            var ownerAccess = RequireOwnerAccess();
            if (ownerAccess is not null)
                return ownerAccess;

            var result = await _accountingInvoiceRequestApiService.MarkDeliveredAsync(documentId, model);

            if (result.IsFailure)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpGet("AccountingInvoiceRequests/Invoice/{invoiceId:int}/Status")]
        public async Task<IActionResult> InvoiceStatus(int invoiceId)
        {
            var ownerAccess = RequireOwnerAccess();
            if (ownerAccess is not null)
                return ownerAccess;

            var result = await _accountingInvoiceRequestApiService.GetInvoiceStatusAsync(invoiceId);

            if (result.IsFailure)
                return BadRequest(result);

            return Ok(result.Data);
        }

        [HttpGet("AccountingInvoiceRequests/OfficialDocument/{documentId:int}/Download")]
        public async Task<IActionResult> DownloadOfficialDocument(int documentId)
        {
            var ownerAccess = RequireOwnerAccess();
            if (ownerAccess is not null)
                return ownerAccess;

            var result = await _accountingInvoiceRequestApiService.DownloadOfficialInvoiceAsync(documentId);

            if (!result.IsSuccess)
            {
                ShowErrors(result.ErrorMessages.Any()
                    ? result.ErrorMessages
                    : new[] { result.ErrorMessage ?? "Fatura dosyası indirilemedi." });

                return RedirectToAction("Index", "Invoices");
            }

            return File(result.Content, result.ContentType, result.FileName);
        }

        [AllowAnonymous]
        [HttpGet("Accounting/InvoiceRequest/{token}")]
        public async Task<IActionResult> Public(string token, [FromQuery] bool uploaded = false, [FromQuery] string? uploadError = null)
        {
            var result = await _accountingInvoiceRequestApiService.GetPublicRequestAsync(token);

            if (result.IsFailure || result.Data is null)
            {
                return View("PublicError", result.ErrorMessage ?? "Fatura hazırlık talebi bulunamadı.");
            }

            var model = ToPublicViewModel(result.Data);
            model.Uploaded = uploaded;
            model.UploadError = uploadError;

            return View(model);
        }

        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        [HttpPost("Accounting/InvoiceRequest/{token}/Upload")]
        [RequestSizeLimit(12 * 1024 * 1024)]
        public async Task<IActionResult> PublicUpload(
            string token,
            [FromForm] PublicInvoiceUploadViewModel model)
        {
            var result = await _accountingInvoiceRequestApiService.UploadSingleAsync(token, model);

            if (result.IsFailure)
            {
                return RedirectToAction(
                    nameof(Public),
                    new
                    {
                        token,
                        uploadError = result.ErrorMessage ?? "Fatura PDF'i yüklenemedi."
                    });
            }

            return RedirectToAction(nameof(Public), new { token, uploaded = true });
        }

        [AllowAnonymous]
        [HttpGet("Accounting/InvoiceUpload/{batchToken}")]
        public async Task<IActionResult> PublicBatch(
            string batchToken,
            [FromQuery] bool uploaded = false,
            [FromQuery] bool completed = false,
            [FromQuery] string? uploadError = null)
        {
            var result = await _accountingInvoiceRequestApiService.GetPublicBatchRequestAsync(batchToken);

            if (result.IsFailure || result.Data is null)
            {
                return View("PublicError", result.ErrorMessage ?? "Fatura yükleme bağlantısı bulunamadı.");
            }

            var model = ToBatchPublicViewModel(result.Data);
            model.ApiBaseUrl = ResolveApiBaseUrl().TrimEnd('/');
            model.ReturnUrl = $"{BuildPublicBaseUrl()}{Request.Path}";
            model.Uploaded = uploaded;
            model.Completed = completed;
            model.UploadError = uploadError;

            return View("PublicBatch", model);
        }

        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        [HttpPost("Accounting/InvoiceUpload/{batchToken}/Items/{requestId:int}/Upload")]
        [RequestSizeLimit(12 * 1024 * 1024)]
        public async Task<IActionResult> PublicBatchUploadItem(
            string batchToken,
            int requestId,
            [FromForm] PublicBatchUploadItemViewModel model)
        {
            var result = await _accountingInvoiceRequestApiService.UploadBatchItemAsync(batchToken, requestId, model);

            if (result.IsFailure)
                return BadRequest(result);

            return Ok(result);
        }

        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        [HttpPost("Accounting/InvoiceUpload/{batchToken}/Complete")]
        public async Task<IActionResult> PublicBatchComplete(string batchToken)
        {
            var result = await _accountingInvoiceRequestApiService.CompleteBatchUploadAsync(batchToken);

            if (result.IsFailure)
                return BadRequest(result);

            return Ok(result);
        }

        [AllowAnonymous]
        [HttpGet("Accounting/InvoiceFile/{shareToken}")]
        public IActionResult PublicInvoiceFile(string shareToken)
        {
            var apiUrl = $"{ResolveApiBaseUrl().TrimEnd('/')}/api/accounting-invoice-requests/public-documents/{Uri.EscapeDataString(shareToken)}/download";
            return Redirect(apiUrl);
        }

        private AccountingInvoiceRequestPublicViewModel ToPublicViewModel(AccountingInvoiceRequestPublicDto dto)
        {
            return new AccountingInvoiceRequestPublicViewModel
            {
                Token = dto.Token,
                InvoiceId = dto.InvoiceId,
                InvoiceNumber = dto.InvoiceNumber,
                InvoiceDate = dto.InvoiceDate,
                StatusText = dto.StatusText,
                CanUpload = dto.CanUpload,
                ExpiresAt = dto.ExpiresAt,
                WorkshopName = dto.WorkshopName,
                WorkshopTaxOffice = dto.WorkshopTaxOffice,
                WorkshopTaxNumber = dto.WorkshopTaxNumber,
                CustomerTitle = dto.CustomerTitle,
                CustomerTaxOffice = dto.CustomerTaxOffice,
                CustomerTaxNumber = dto.CustomerTaxNumber,
                CustomerTckn = dto.CustomerTckn,
                CustomerAddress = dto.CustomerAddress,
                Plate = dto.Plate,
                VehicleText = dto.VehicleText,
                VehicleBrandName = dto.VehicleBrandName,
                VehicleModelName = dto.VehicleModelName,
                VehicleVariantName = dto.VehicleVariantName,
                VehicleModelYear = dto.VehicleModelYear,
                Mileage = dto.Mileage,
                ChassisNumber = dto.ChassisNumber,
                Subtotal = dto.Subtotal,
                DiscountTotal = dto.DiscountTotal,
                VatTotal = dto.VatTotal,
                GrandTotal = dto.GrandTotal,
                PaidTotal = dto.PaidTotal,
                RemainingAmount = dto.RemainingAmount,
                Items = dto.Items,
                OfficialInvoiceDocument = dto.OfficialInvoiceDocument
            };
        }

        private AccountingInvoiceBatchPublicViewModel ToBatchPublicViewModel(AccountingInvoiceBatchPublicDto dto)
        {
            return new AccountingInvoiceBatchPublicViewModel
            {
                BatchToken = dto.BatchToken,
                WorkshopName = dto.WorkshopName,
                RecipientEmail = dto.RecipientEmail,
                Message = dto.Message,
                SentAt = dto.SentAt,
                ExpiresAt = dto.ExpiresAt,
                CanUpload = dto.CanUpload,
                TotalCount = dto.TotalCount,
                UploadedCount = dto.UploadedCount,
                PendingCount = dto.PendingCount,
                StatusText = dto.StatusText,
                Items = dto.Items
            };
        }

        private string BuildPublicBaseUrl()
        {
            return $"{Request.Scheme}://{Request.Host}";
        }

        private string ResolveApiBaseUrl()
        {
            return _configuration["ApiSettings:BaseUrl"]
                   ?? _configuration["ApiBaseUrl"]
                   ?? _configuration["BaseUrl"]
                   ?? throw new InvalidOperationException("API BaseUrl configuration bulunamadı.");
        }
    }
}
