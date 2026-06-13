using AutoStock.Services.Dtos.Common;
using AutoStock.Services.Dtos.Invoices;

namespace AutoStock.Services.Interfaces;

public interface IInvoiceEmailService
{
    Task<ServiceResult<SendInvoiceEmailResponseDto>> SendInvoiceAsync(
        int invoiceId,
        int workshopId,
        SendInvoiceEmailRequestDto request,
        CancellationToken cancellationToken = default);
}
