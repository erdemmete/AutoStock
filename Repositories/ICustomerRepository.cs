using AutoStock.Repositories.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace AutoStock.Repositories
{
    public interface ICustomerRepository:ICustomerRepository<Customer>
    {
        public Task<IEnumerable<Customer>> GetCustomersWithVehicles();
    }
}
