using AutoStock.Repositories.Entities;

namespace AutoStock.Repositories
{
    public interface ICustomerRepository : IGenericRepository<Customer>
    {
        public Task<List<Customer>> GetCustomersWithVehicles(int count);
    }
}
