using AutoStock.Repositories;
using AutoStock.Repositories.Entities;
using AutoStock.Services.Dtos.Common;
using AutoStock.Services.Interfaces;
using System.Net;

namespace AutoStock.Services.Services
{
    public class CustomerService(ICustomerRepository customerRepository) : ICustomerService
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
                return ServiceResult<Customer>.Fail("Müşteri bulunamadı", HttpStatusCode.NotFound);
            }

            return ServiceResult<Customer>.Success(customer);
        }
    }
}
