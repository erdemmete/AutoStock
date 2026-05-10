using AutoStock.Services.Dtos.Customers;

namespace AutoStock.Services.Interfaces
{
    public interface ICustomerService
    {
        Task<List<CustomerSearchDto>> SearchAsync(string query, int workshopId);
    }
}
