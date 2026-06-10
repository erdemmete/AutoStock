using AutoStock.Services.Dtos.Common;
using AutoStock.Services.Dtos.Invoices;

namespace AutoStock.Services.Interfaces
{
    public interface IInvoiceService
    {
        Task<ServiceResult<CreateInvoiceDraftDto>> GetCreateDraftAsync(int serviceRecordId, int workshopId);

        Task<ServiceResult<CreateInvoiceResponseDto>> CreateAsync(CreateInvoiceDto request, int workshopId);


        Task<ServiceResult<InvoiceDetailDto>> GetDetailAsync(int invoiceId, int workshopId);

        Task<ServiceResult<IssueInvoiceResponseDto>> IssueAsync(int invoiceId, int workshopId);


        Task<ServiceResult<List<InvoiceListItemDto>>> GetListAsync(int workshopId);

        Task<ServiceResult<List<InvoiceListItemDto>>> GetListByServiceRecordAsync(int serviceRecordId, int workshopId);

        Task<ServiceResult<InvoiceDetailDto>> GetDraftByServiceRecordAsync(int serviceRecordId, int workshopId);
        Task<ServiceResult<bool>> SyncDraftByServiceRecordAsync(
    int serviceRecordId,
    int workshopId);

        Task<ServiceResult<InvoiceNavigationDto>> GetActiveInvoiceByServiceRecordAsync(int serviceRecordId, int workshopId);
        Task<ServiceResult<CancelInvoiceResponseDto>> CancelAsync(int invoiceId, int workshopId);

        Task<ServiceResult<InvoiceDetailDto>> UpdateAsync(UpdateInvoiceDto request, int workshopId);
        Task<ServiceResult<PagedResult<InvoiceListItemDto>>> GetPagedAsync(InvoiceListQueryDto query, int workshopId);
    }

}