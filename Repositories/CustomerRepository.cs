using AutoStock.Repositories.Entities;
using Microsoft.EntityFrameworkCore;

namespace AutoStock.Repositories
{
    public class CustomerRepository(AppDbContext context) : GenericRepository<Customer>(context), ICustomerRepository
    {
        public async Task<IEnumerable<Customer>> GetCustomersWithVehicles()
        {
            return await context.Customers.Include(c => c.Vehicles).ToListAsync();
        }

        public Task<List<Customer>> GetCustomersWithVehicles(int count)
        {
            throw new NotImplementedException();
        }
    }
}
