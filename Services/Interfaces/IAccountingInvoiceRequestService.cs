using AutoStock.Services.Dtos.Accounting;
using AutoStock.Services.Dtos.Common;

namespace AutoStock.Services.Interfaces
{
    public interface IAccountingInvoiceRequestService
    {
        Task<ServiceResult<List<AccountingEmailRecipientDto>>> GetAccountingRecipientsAsync(int workshopId);

        Task<ServiceResult<AccountingEmailRecipientDto>> SaveAccountingRecipientAsync(
            CreateAccountingEmailRecipientDto request,
            int workshopId);

        Task<ServiceResult<SendAccountingInvoiceRequestResponseDto>> SendAccountingRequestAsync(
            SendAccountingInvoiceRequestDto request,
            int workshopId);

        Task<ServiceResult<AccountingInvoiceRequestPublicDto>> GetPublicRequestAsync(string token);

        Task<ServiceResult<OfficialInvoiceDocumentDto>> UploadOfficialInvoiceAsync(
            string token,
            UploadOfficialInvoiceDto request);

        Task<ServiceResult<InvoiceAccountingStatusDto>> GetInvoiceAccountingStatusAsync(
            int invoiceId,
            int workshopId);

        Task<ServiceResult<OfficialInvoiceFileDto>> GetOfficialInvoiceFileAsync(
            int officialInvoiceDocumentId,
            int workshopId);
    }
}
