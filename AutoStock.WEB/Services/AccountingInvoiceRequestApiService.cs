using AutoStock.Services.Dtos.Accounting;
using AutoStock.WEB.Models.Accounting;
using AutoStock.WEB.Models.Common;
using System.Net.Http.Headers;

namespace AutoStock.WEB.Services
{
    public class AccountingInvoiceRequestApiService : BaseApiService
    {
        public AccountingInvoiceRequestApiService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor,
            ILogger<AccountingInvoiceRequestApiService> logger)
            : base(httpClientFactory, configuration, httpContextAccessor, logger)
        {
        }

        public async Task<ApiResponse<List<AccountingEmailRecipientDto>>> GetRecipientsAsync()
        {
            return await GetAsync<List<AccountingEmailRecipientDto>>(
                "/api/accounting-invoice-requests/recipients",
                "Fatura hazırlık alıcıları alınırken hata oluştu.");
        }

        public async Task<ApiResponse<AccountingEmailRecipientDto>> SaveRecipientAsync(CreateAccountingEmailRecipientDto model)
        {
            return await PostJsonAsync<CreateAccountingEmailRecipientDto, AccountingEmailRecipientDto>(
                "/api/accounting-invoice-requests/recipients",
                model,
                "Fatura hazırlık alıcısı kaydedilirken hata oluştu.");
        }

        public async Task<ApiResponse<SendAccountingInvoiceRequestResponseDto>> SendAsync(SendAccountingInvoiceRequestViewModel model)
        {
            return await PostJsonAsync<SendAccountingInvoiceRequestDto, SendAccountingInvoiceRequestResponseDto>(
                "/api/accounting-invoice-requests/send",
                model.ToDto(),
                "Fatura hazırlığına gönderilirken hata oluştu.");
        }

        public async Task<ApiResponse<SendAccountingInvoiceBatchResponseDto>> SendBatchAsync(SendAccountingInvoiceBatchRequestViewModel model)
        {
            return await PostJsonAsync<SendAccountingInvoiceBatchRequestDto, SendAccountingInvoiceBatchResponseDto>(
                "/api/accounting-invoice-requests/send-batch",
                model.ToDto(),
                "Fatura hazırlığına gönderilirken hata oluştu.");
        }

        public async Task<ApiResponse<InvoiceAccountingStatusDto>> GetInvoiceStatusAsync(int invoiceId)
        {
            return await GetAsync<InvoiceAccountingStatusDto>(
                $"/api/accounting-invoice-requests/invoices/{invoiceId}/status",
                "Fatura durumu alınırken hata oluştu.");
        }

        public async Task<ApiResponse<AccountingInvoiceRequestPublicDto>> GetPublicRequestAsync(string token)
        {
            return await GetAsync<AccountingInvoiceRequestPublicDto>(
                $"/api/accounting-invoice-requests/public/{Uri.EscapeDataString(token)}",
                "Fatura hazırlık talebi alınırken hata oluştu.");
        }

        public async Task<ApiResponse<AccountingInvoiceBatchPublicDto>> GetPublicBatchRequestAsync(string batchToken)
        {
            return await GetAsync<AccountingInvoiceBatchPublicDto>(
                $"/api/accounting-invoice-requests/public/batches/{Uri.EscapeDataString(batchToken)}",
                "Fatura yükleme bilgileri alınırken hata oluştu.");
        }

        public async Task<ApiResponse<OfficialInvoiceDocumentDto>> MarkDeliveredAsync(int documentId, MarkOfficialInvoiceDeliveredDto model)
        {
            return await PostJsonAsync<MarkOfficialInvoiceDeliveredDto, OfficialInvoiceDocumentDto>(
                $"/api/accounting-invoice-requests/official-documents/{documentId}/mark-delivered",
                model,
                "İletim bilgisi kaydedilirken hata oluştu.");
        }

        public async Task<OfficialInvoiceDownloadResult> DownloadOfficialInvoiceAsync(int documentId)
        {
            try
            {
                var client = CreateApiClient();
                using var response = await client.GetAsync($"/api/accounting-invoice-requests/official-documents/{documentId}/download");

                if (!response.IsSuccessStatusCode)
                {
                    var apiError = await ReadApiResponseAsync<object>(
                        response,
                        "Fatura dosyası indirilemedi.");

                    return OfficialInvoiceDownloadResult.Fail(apiError.ErrorMessage, apiError.ErrorMessages);
                }

                var content = await response.Content.ReadAsByteArrayAsync();
                var fileName = ResolveFileName(response.Content.Headers.ContentDisposition)
                    ?? $"fatura-{documentId}.pdf";

                var contentType = response.Content.Headers.ContentType?.MediaType
                    ?? "application/pdf";

                return OfficialInvoiceDownloadResult.Success(content, fileName, contentType);
            }
            catch (Exception ex)
            {
                return OfficialInvoiceDownloadResult.Fail($"Fatura dosyası indirilemedi: {ex.Message}");
            }
        }

        private static string? ResolveFileName(ContentDispositionHeaderValue? contentDisposition)
        {
            if (contentDisposition is null)
                return null;

            var fileName = contentDisposition.FileNameStar;

            if (string.IsNullOrWhiteSpace(fileName))
                fileName = contentDisposition.FileName;

            return string.IsNullOrWhiteSpace(fileName)
                ? null
                : fileName.Trim().Trim('"');
        }
    }
}
