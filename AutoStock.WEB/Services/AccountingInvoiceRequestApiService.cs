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

        public async Task<ApiResponse<OfficialInvoiceDocumentDto>> UploadSingleAsync(
            string token,
            PublicInvoiceUploadViewModel model)
        {
            if (model.File is null || model.File.Length == 0)
                return ApiResponse<OfficialInvoiceDocumentDto>.Fail("PDF dosyası seçiniz.");

            return await SendAsync<OfficialInvoiceDocumentDto>(
                $"/api/accounting-invoice-requests/public/{Uri.EscapeDataString(token)}/upload",
                async client =>
                {
                    using var content = new MultipartFormDataContent();
                    content.Add(new StringContent(model.OfficialInvoiceNumber ?? string.Empty), "officialInvoiceNumber");
                    content.Add(new StringContent(model.OfficialInvoiceDate.ToString("yyyy-MM-dd")), "officialInvoiceDate");
                    content.Add(new StringContent(model.UploadedByEmail ?? string.Empty), "uploadedByEmail");

                    if (!string.IsNullOrWhiteSpace(model.EttnOrUuid))
                        content.Add(new StringContent(model.EttnOrUuid), "ettnOrUuid");

                    if (!string.IsNullOrWhiteSpace(model.Note))
                        content.Add(new StringContent(model.Note), "note");

                    await using var stream = model.File.OpenReadStream();
                    using var fileContent = new StreamContent(stream);
                    fileContent.Headers.ContentType = new MediaTypeHeaderValue(string.IsNullOrWhiteSpace(model.File.ContentType)
                        ? "application/pdf"
                        : model.File.ContentType);
                    content.Add(fileContent, "file", model.File.FileName);

                    client.DefaultRequestHeaders.TryAddWithoutValidation("X-Requested-With", "XMLHttpRequest");
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    return await client.PostAsync(
                        $"/api/accounting-invoice-requests/public/{Uri.EscapeDataString(token)}/upload",
                        content);
                },
                "Fatura PDF'i yüklenirken hata oluştu.");
        }

        public async Task<ApiResponse<OfficialInvoiceDocumentDto>> UploadBatchItemAsync(
            string batchToken,
            int requestId,
            PublicBatchUploadItemViewModel model)
        {
            if (model.File is null || model.File.Length == 0)
                return ApiResponse<OfficialInvoiceDocumentDto>.Fail("PDF dosyası seçiniz.");

            return await SendAsync<OfficialInvoiceDocumentDto>(
                $"/api/accounting-invoice-requests/public/batches/{Uri.EscapeDataString(batchToken)}/items/{requestId}/upload",
                async client =>
                {
                    using var content = new MultipartFormDataContent();
                    content.Add(new StringContent(model.OfficialInvoiceNumber ?? string.Empty), "officialInvoiceNumber");
                    content.Add(new StringContent(model.OfficialInvoiceDate.ToString("yyyy-MM-dd")), "officialInvoiceDate");
                    content.Add(new StringContent(model.UploadedByEmail ?? string.Empty), "uploadedByEmail");

                    if (!string.IsNullOrWhiteSpace(model.Note))
                        content.Add(new StringContent(model.Note), "note");

                    await using var stream = model.File.OpenReadStream();
                    using var fileContent = new StreamContent(stream);
                    fileContent.Headers.ContentType = new MediaTypeHeaderValue(string.IsNullOrWhiteSpace(model.File.ContentType)
                        ? "application/pdf"
                        : model.File.ContentType);
                    content.Add(fileContent, "file", model.File.FileName);

                    client.DefaultRequestHeaders.TryAddWithoutValidation("X-Requested-With", "XMLHttpRequest");
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    return await client.PostAsync(
                        $"/api/accounting-invoice-requests/public/batches/{Uri.EscapeDataString(batchToken)}/items/{requestId}/upload",
                        content);
                },
                "Fatura PDF'i yüklenirken hata oluştu.");
        }

        public async Task<ApiResponse<CompleteAccountingInvoiceBatchUploadResponseDto>> CompleteBatchUploadAsync(string batchToken)
        {
            return await SendAsync<CompleteAccountingInvoiceBatchUploadResponseDto>(
                $"/api/accounting-invoice-requests/public/batches/{Uri.EscapeDataString(batchToken)}/complete",
                client =>
                {
                    client.DefaultRequestHeaders.TryAddWithoutValidation("X-Requested-With", "XMLHttpRequest");
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    return client.PostAsync(
                        $"/api/accounting-invoice-requests/public/batches/{Uri.EscapeDataString(batchToken)}/complete",
                        new FormUrlEncodedContent(new Dictionary<string, string>()));
                },
                "Yükleme durumu tamamlanırken hata oluştu.");
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
