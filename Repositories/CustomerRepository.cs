using AutoStock.Repositories.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace AutoStock.Repositories
{
    public class CustomerRepository(AppDbContext context) : GenericRepository<Customer>(context), ICustomerRepository
    {
        public async Task<IEnumerable<Customer>> GetCustomersWithVehicles()
        {
            return await context.Customers.Include(c => c.Vehicles).ToListAsync();
        }
    }
}
