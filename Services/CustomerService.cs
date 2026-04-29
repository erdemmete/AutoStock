using AutoStock.Repositories;
using AutoStock.Repositories.Entities;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace AutoStock.Services
{
    public class CustomerService(ICustomerRepository customerRepository): ICustomerService
    {
        public async Task<ServiceResult<List<Customer>>> GetCustomersWithVehicles(int count)
        {
           var costumers = await customerRepository.GetCustomersWithVehicles(count);
            return ServiceResult<List<Customer>>.Success(costumers);
        }

        public async Task<ServiceResult<Customer>> GetCustomerById(int id)
        {
            var customer = await customerRepository.GetByIdAsync(id);
            if (customer is null)
            {
                ServiceResult<Customer>.Fail("Müşteri bulunamadı",HttpStatusCode.NotFound);
            }

            return ServiceResult<Customer>.Success(customer!);
        }
    }
}
