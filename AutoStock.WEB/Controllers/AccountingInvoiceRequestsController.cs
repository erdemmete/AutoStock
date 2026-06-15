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
            var result = await _accountingInvoiceRequestApiService.GetRecipientsAsync();

            if (result.IsFailure)
                return BadRequest(result);

            return Ok(result.Data ?? new List<AccountingEmailRecipientDto>());
        }

        [HttpPost("AccountingInvoiceRequests/Recipients")]
        public async Task<IActionResult> SaveRecipient([FromBody] CreateAccountingEmailRecipientDto model)
        {
            var result = await _accountingInvoiceRequestApiService.SaveRecipientAsync(model);

            if (result.IsFailure)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPost("AccountingInvoiceRequests/Send")]
        public async Task<IActionResult> Send([FromBody] SendAccountingInvoiceRequestViewModel model)
        {
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

        [HttpGet("AccountingInvoiceRequests/Invoice/{invoiceId:int}/Status")]
        public async Task<IActionResult> InvoiceStatus(int invoiceId)
        {
            var result = await _accountingInvoiceRequestApiService.GetInvoiceStatusAsync(invoiceId);

            if (result.IsFailure)
                return BadRequest(result);

            return Ok(result.Data);
        }

        [HttpGet("AccountingInvoiceRequests/OfficialDocument/{documentId:int}/Download")]
        public async Task<IActionResult> DownloadOfficialDocument(int documentId)
        {
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
            model.UploadApiUrl = $"{ResolveApiBaseUrl().TrimEnd('/')}/api/accounting-invoice-requests/public/{Uri.EscapeDataString(token)}/upload";
            model.ReturnUrl = $"{BuildPublicBaseUrl()}{Request.Path}";
            model.Uploaded = uploaded;
            model.UploadError = uploadError;

            return View(model);
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
