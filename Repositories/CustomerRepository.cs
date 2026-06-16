using AutoStock.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AutoStock.Repositories
{
    public class CustomerRepository : GenericRepository<Customer>, ICustomerRepository
    {
        private readonly AppDbContext _context;

        public CustomerRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Customer>> GetCustomersWithVehicles()
        {
            return await _context.Customers.Include(c => c.Vehicles).ToListAsync();
        }

        public Task<List<Customer>> GetCustomersWithVehicles(int count)
        {
            throw new NotImplementedException();
        }

        public async Task<List<Customer>> SearchAsync(string query, int workshopId)
        {
            query = query.Trim().ToLower();

            return await _context.Customers
                .Where(x =>
                    x.WorkshopId == workshopId &&
                    (
                        x.FullName!.ToLower().Contains(query) ||
                        (x.CompanyName != null && x.CompanyName.ToLower().Contains(query)) ||
                        x.PhoneNumber.Contains(query) ||
                        (x.Email != null && x.Email.ToLower().Contains(query))
                    ))
                .OrderBy(x => x.FullName)
                .Take(10)
                .ToListAsync();
        }
    }
}
