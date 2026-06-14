using AutoStock.Services.Dtos.Common;
using AutoStock.Services.Dtos.CurrentAccounts;

namespace AutoStock.Services.Interfaces
{
    public interface ICurrentAccountService
    {
        Task<ServiceResult<bool>> CreatePaymentAsync(CreatePaymentRequestDto request, int workshopId);

        Task<ServiceResult<bool>> CancelPaymentAsync(int transactionId, CancelPaymentRequestDto request, int workshopId);

        Task<ServiceResult<GetCustomerCurrentAccountResponseDto>> GetCustomerAccountAsync(int customerId, int workshopId);

        Task<ServiceResult<CurrentAccountSummaryDto>> GetSummaryAsync(int workshopId);

        Task<ServiceResult<CurrentAccountPagedSummaryDto>> GetPagedSummaryAsync(CurrentAccountListQueryDto query, int workshopId);
    }
}
