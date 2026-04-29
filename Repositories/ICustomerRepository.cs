using AutoStock.Repositories.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace AutoStock.Repositories
{
    public interface ICustomerRepository:IGenericRepository<Customer>
    {
        public Task<IEnumerable<Customer>> GetCustomersWithVehicles();
    }
}
