using AutoStock.Services.Dtos.Common;
using AutoStock.Services.Dtos.Invoices;

namespace AutoStock.Services.Interfaces
{
    public interface IInvoiceService
    {
        Task<ServiceResult<CreateInvoiceDraftDto>> GetCreateDraftAsync(
            int serviceRecordId,
            int workshopId);

        Task<ServiceResult<CreateInvoiceResponseDto>> CreateAsync(
    CreateInvoiceDto request,
    int workshopId);
    }
}