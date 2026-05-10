using AutoStock.Repositories.Entities;

namespace AutoStock.Repositories
{
    public interface ICustomerRepository : IGenericRepository<Customer>
    {
        public Task<List<Customer>> GetCustomersWithVehicles(int count);
        Task<List<Customer>> SearchAsync(string query, int workshopId);
    }
}
