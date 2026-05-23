using AutoStock.Services.Dtos.Common;
using AutoStock.Services.Dtos.Customers;

namespace AutoStock.Services.Interfaces
{
    public interface ICustomerService
    {
        Task<List<CustomerSearchDto>> SearchAsync(string query, int workshopId);
        Task<ServiceResult<List<CustomerListItemDto>>> GetListAsync(int workshopId);
        Task<ServiceResult<int>> CreateAsync(CreateCustomerDto request, int workshopId);
        Task<ServiceResult<CustomerDetailDto>> GetByIdAsync(int id, int workshopId);
        Task<ServiceResult<int>> UpdateAsync(UpdateCustomerDto request, int workshopId);
        Task<ServiceResult<int>> SetPassiveAsync(int id, int workshopId);
    }
}
