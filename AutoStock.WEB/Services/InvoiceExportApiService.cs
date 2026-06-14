using AutoStock.Services.Dtos.Invoices;
using AutoStock.WEB.Models.Common;
using AutoStock.WEB.Models.Invoices;
using System.Net.Http.Headers;

namespace AutoStock.WEB.Services;

public class InvoiceExportApiService : BaseApiService
{
    public InvoiceExportApiService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        IHttpContextAccessor httpContextAccessor,
        ILogger<InvoiceExportApiService> logger)
        : base(httpClientFactory, configuration, httpContextAccessor, logger)
    {
    }

    public async Task<ApiResponse<InvoiceExportPreviewDto>> GetPreviewAsync(InvoiceExportQueryViewModel query)
    {
        query.Normalize();

        return await GetAsync<InvoiceExportPreviewDto>(
            BuildExportUrl("/api/invoices/export/preview", query),
            "Fatura aktarım önizlemesi alınırken hata oluştu.");
    }

    public async Task<InvoiceExportDownloadResult> DownloadAsync(InvoiceExportQueryViewModel query)
    {
        query.Normalize();

        var url = BuildExportUrl("/api/invoices/export/download", query);

        try
        {
            var client = CreateApiClient();
            using var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var apiError = await ReadApiResponseAsync<object>(
                    response,
                    "Fatura aktarım paketi indirilemedi.");

                return InvoiceExportDownloadResult.Fail(
                    apiError.ErrorMessage,
                    apiError.ErrorMessages);
            }

            var content = await response.Content.ReadAsByteArrayAsync();
            var fileName = ResolveFileName(response.Content.Headers.ContentDisposition)
                ?? $"fatura-aktarim-{DateTime.Now:yyyyMMddHHmm}.zip";

            var contentType = response.Content.Headers.ContentType?.MediaType
                ?? "application/zip";

            return InvoiceExportDownloadResult.Success(content, fileName, contentType);
        }
        catch (Exception ex)
        {
            return InvoiceExportDownloadResult.Fail(
                $"Fatura aktarım paketi indirilemedi: {ex.Message}");
        }
    }

    public async Task<ApiResponse<object>> SendEmailAsync(InvoiceExportQueryViewModel query)
    {
        query.Normalize();

        return await PostJsonAsync<SendInvoiceExportEmailRequestDto, object>(
            "/api/invoices/export/send-email",
            query.ToEmailDto(),
            "Fatura aktarım e-postası gönderilirken hata oluştu.");
    }

    private static string BuildExportUrl(string baseUrl, InvoiceExportQueryViewModel query)
    {
        return BuildUrlWithQuery(baseUrl, new Dictionary<string, string?>
        {
            ["startDate"] = query.StartDate?.ToString("yyyy-MM-dd"),
            ["endDate"] = query.EndDate?.ToString("yyyy-MM-dd"),
            ["preset"] = query.Preset,
            ["includeCancelled"] = query.IncludeCancelled.ToString().ToLowerInvariant()
        });
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
