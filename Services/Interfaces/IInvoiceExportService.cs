using AutoStock.Services.Dtos.Common;
using AutoStock.Services.Dtos.Invoices;

namespace AutoStock.Services.Interfaces;

public interface IInvoiceExportService
{
    Task<ServiceResult<InvoiceExportPreviewDto>> GetPreviewAsync(InvoiceExportQueryDto query, int workshopId);

    Task<ServiceResult<InvoiceExportFileDto>> CreateZipAsync(InvoiceExportQueryDto query, int workshopId);

    Task<ServiceResult<bool>> SendEmailAsync(SendInvoiceExportEmailRequestDto request, int workshopId);
}
