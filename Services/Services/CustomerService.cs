using AutoStock.Repositories;
using AutoStock.Services.Dtos.Common;
using AutoStock.Services.Dtos.Customers;
using AutoStock.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace AutoStock.Services.Services
{
    public class CustomerService(
    ICustomerRepository customerRepository,
    AppDbContext context) : ICustomerService
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

        public async Task<List<CustomerSearchDto>> SearchAsync(string query, int workshopId)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new List<CustomerSearchDto>();

            var customers = await customerRepository.SearchAsync(query, workshopId);

            return customers.Select(x => new CustomerSearchDto
            {
                Id = x.Id,
                Name = !string.IsNullOrWhiteSpace(x.FullName)
                    ? x.FullName
                    : x.CompanyName ?? "-",
                PhoneNumber = x.PhoneNumber,
                Email = x.Email
            }).ToList();
        }

        public async Task<ServiceResult<List<CustomerListItemDto>>> GetListAsync(int workshopId)
        {
            var customers = await context.Customers
                .Where(x => x.WorkshopId == workshopId)
                .Select(x => new CustomerListItemDto
                {
                    Id = x.Id,

                    DisplayName = !string.IsNullOrWhiteSpace(x.FullName)
                        ? x.FullName
                        : x.CompanyName ?? "İsimsiz Müşteri",

                    PhoneNumber = x.PhoneNumber,
                    Email = x.Email,

                    TypeText = x.Type.ToString(),

                    Balance = context.CurrentAccountTransactions
                        .Where(t =>
                            t.WorkshopId == workshopId &&
                            t.CustomerId == x.Id)
                        .Sum(t => t.Debit - t.Credit)
                })
                .OrderBy(x => x.DisplayName)
                .ToListAsync();

            return ServiceResult<List<CustomerListItemDto>>.Success(customers);
        }
        public async Task<ServiceResult<int>> CreateAsync(CreateCustomerDto request, int workshopId)
        {
            if (string.IsNullOrWhiteSpace(request.PhoneNumber))
                return ServiceResult<int>.Fail("Telefon numarası zorunludur.");

            if (string.IsNullOrWhiteSpace(request.FullName))
                return ServiceResult<int>.Fail("Müşteri / cari adı zorunludur.");

            var customer = new Customer
            {
                WorkshopId = workshopId,
                Type = request.Type,

                PhoneNumber = request.PhoneNumber.Trim(),
                FullName = request.FullName?.Trim(),
                CompanyName = request.CompanyName?.Trim(),
                AuthorizedPersonName = request.AuthorizedPersonName?.Trim(),
                Email = request.Email?.Trim(),

                TaxOffice = request.TaxOffice?.Trim(),
                TaxNumber = request.TaxNumber?.Trim(),
                NationalIdentityNumber = request.NationalIdentityNumber?.Trim(),

                Address = request.Address?.Trim(),
                AddressDistrict = request.AddressDistrict?.Trim(),
                AddressCity = request.AddressCity?.Trim()
            };

            context.Customers.Add(customer);
            await context.SaveChangesAsync();

            return ServiceResult<int>.Success(customer.Id);
        }
    }
}
