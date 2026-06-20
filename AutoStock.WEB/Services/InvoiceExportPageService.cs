using AutoStock.Services.Dtos.Invoices;
using AutoStock.WEB.Models.Common;
using AutoStock.WEB.Models.Invoices;

namespace AutoStock.WEB.Services;

public class InvoiceExportPageService
{
    private readonly InvoiceExportApiService _invoiceExportApiService;

    public InvoiceExportPageService(InvoiceExportApiService invoiceExportApiService)
    {
        _invoiceExportApiService = invoiceExportApiService;
    }

    public async Task<PageViewResult<InvoiceExportIndexViewModel>> BuildIndexAsync(InvoiceExportQueryViewModel? query)
    {
        query ??= new InvoiceExportQueryViewModel();
        query.Normalize();

        var previewResult = await _invoiceExportApiService.GetPreviewAsync(query);

        var model = new InvoiceExportIndexViewModel
        {
            Query = query,
            Preview = previewResult.Data ?? new InvoiceExportPreviewDto
            {
                StartDate = query.StartDate ?? DateTime.Today,
                EndDate = query.EndDate ?? DateTime.Today,
                PeriodText = query.StartDate.HasValue && query.EndDate.HasValue
                    ? $"{query.StartDate:dd.MM.yyyy} - {query.EndDate:dd.MM.yyyy}"
                    : "Seçilen dönem",
                IncludeCancelled = query.IncludeCancelled
            }
        };

        if (previewResult.IsFailure)
        {
            return PageViewResult<InvoiceExportIndexViewModel>.WithErrors(
                model,
                previewResult.ErrorMessages.Any()
                    ? previewResult.ErrorMessages
                    : new[] { previewResult.ErrorMessage ?? "Belgeler alınırken hata oluştu." });
        }

        return PageViewResult<InvoiceExportIndexViewModel>.Success(model);
    }
}
