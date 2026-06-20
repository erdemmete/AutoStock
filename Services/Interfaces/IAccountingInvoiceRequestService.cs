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
            int workshopId,
            int requestedByUserId);

        Task<ServiceResult<SendAccountingInvoiceBatchResponseDto>> SendAccountingBatchRequestAsync(
            SendAccountingInvoiceBatchRequestDto request,
            int workshopId,
            int requestedByUserId);

        Task<ServiceResult<AccountingInvoiceRequestPublicDto>> GetPublicRequestAsync(string token);

        Task<ServiceResult<AccountingInvoiceBatchPublicDto>> GetPublicBatchRequestAsync(string batchToken);

        Task<ServiceResult<OfficialInvoiceDocumentDto>> UploadOfficialInvoiceAsync(
            string token,
            UploadOfficialInvoiceDto request);

        Task<ServiceResult<OfficialInvoiceDocumentDto>> UploadOfficialInvoiceForBatchItemAsync(
            string batchToken,
            int accountingRequestId,
            UploadOfficialInvoiceDto request);

        Task<ServiceResult<CompleteAccountingInvoiceBatchUploadResponseDto>> CompleteBatchUploadAsync(
            string batchToken);

        Task<ServiceResult<InvoiceAccountingStatusDto>> GetInvoiceAccountingStatusAsync(
            int invoiceId,
            int workshopId);

        Task<ServiceResult<OfficialInvoiceFileDto>> GetOfficialInvoiceFileAsync(
            int officialInvoiceDocumentId,
            int workshopId);

        Task<ServiceResult<OfficialInvoiceFileDto>> GetOfficialInvoiceFileByShareTokenAsync(string shareToken);

        Task<ServiceResult<OfficialInvoiceDocumentDto>> MarkOfficialInvoiceDeliveredAsync(
            int officialInvoiceDocumentId,
            int workshopId,
            int userId,
            MarkOfficialInvoiceDeliveredDto request);
    }
}
