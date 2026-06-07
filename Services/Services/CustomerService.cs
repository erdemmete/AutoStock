using AutoStock.Repositories;
using AutoStock.Repositories.Enums;
using AutoStock.Services.Dtos.AuditLogs;
using AutoStock.Services.Dtos.Common;
using AutoStock.Services.Dtos.Customers;
using AutoStock.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace AutoStock.Services.Services
{
    public class CustomerService(
    ICustomerRepository customerRepository,
    AppDbContext context,
    IAuditLogService auditLogService) : ICustomerService
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
                return ServiceResult<Customer>.Fail("Müşteri bulunamadı");
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
                .Where(x => x.WorkshopId == workshopId && x.IsActive)
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

        public async Task<ServiceResult<PagedResult<CustomerListItemDto>>> GetPagedAsync(CustomerListQueryDto query, int workshopId)
        {
            query ??= new CustomerListQueryDto();
            query.Normalize();

            var customersQuery = context.Customers
                    .AsNoTracking()
                    .Where(x => x.WorkshopId == workshopId && x.IsActive);

            if (query.Type.HasValue)
            {
                customersQuery = customersQuery.Where(x => x.Type == query.Type.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = $"%{query.Search}%";

                customersQuery = customersQuery.Where(x =>
                    EF.Functions.Like(x.FullName ?? string.Empty, search) ||
                    EF.Functions.Like(x.CompanyName ?? string.Empty, search) ||
                    EF.Functions.Like(x.AuthorizedPersonName ?? string.Empty, search) ||
                    EF.Functions.Like(x.PhoneNumber ?? string.Empty, search) ||
                    EF.Functions.Like(x.Email ?? string.Empty, search) ||
                    EF.Functions.Like(x.NationalIdentityNumber ?? string.Empty, search) ||
                    EF.Functions.Like(x.TaxNumber ?? string.Empty, search));
            }

            var totalCount = await customersQuery.CountAsync();

            var items = await customersQuery
                .OrderBy(x => x.FullName ?? x.CompanyName ?? x.PhoneNumber)
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
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
                        .Where(t => t.WorkshopId == workshopId && t.CustomerId == x.Id)
                        .Sum(t => t.Debit - t.Credit)
                })
                .ToListAsync();

            var result = new PagedResult<CustomerListItemDto>
            {
                Items = items,
                PageNumber = query.PageNumber,
                PageSize = query.PageSize,
                TotalCount = totalCount
            };

            return ServiceResult<PagedResult<CustomerListItemDto>>.Success(result);
        }
        public async Task<ServiceResult<int>> CreateAsync(CreateCustomerDto request, int workshopId)
        {
            await using var transaction = await context.Database.BeginTransactionAsync();

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

            await auditLogService.AddAsync(new AuditLogCreateDto
            {
                WorkshopId = workshopId,
                ActionType = AuditActionType.Create,
                EntityType = AuditEntityType.Customer,
                EntityId = customer.Id,
                Description = $"Müşteri oluşturuldu: {GetCustomerDisplayName(customer)}",
                NewValues = new
                {
                    customer.Type,
                    DisplayName = GetCustomerDisplayName(customer)
                }
            });

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            return ServiceResult<int>.Success(customer.Id);
        }

        public async Task<ServiceResult<CustomerDetailDto>> GetByIdAsync(int id, int workshopId)
        {
            var customer = await context.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                         x.Id == id &&
                         x.WorkshopId == workshopId &&
                         x.IsActive);

            if (customer == null)
                return ServiceResult<CustomerDetailDto>.Fail("Müşteri bulunamadı.");

            var dto = new CustomerDetailDto
            {
                Id = customer.Id,
                WorkshopId = customer.WorkshopId,

                Type = customer.Type,

                PhoneNumber = customer.PhoneNumber,

                FullName = customer.FullName,
                CompanyName = customer.CompanyName,
                AuthorizedPersonName = customer.AuthorizedPersonName,

                Email = customer.Email,

                TaxOffice = customer.TaxOffice,
                TaxNumber = customer.TaxNumber,
                NationalIdentityNumber = customer.NationalIdentityNumber,

                Address = customer.Address,
                AddressCity = customer.AddressCity,
                AddressDistrict = customer.AddressDistrict,

                IsActive = customer.IsActive
            };

            return ServiceResult<CustomerDetailDto>.Success(dto);
        }
        public async Task<ServiceResult<int>> UpdateAsync(UpdateCustomerDto request, int workshopId)
        {
            var customer = await context.Customers
                .FirstOrDefaultAsync(x =>
                x.Id == request.Id &&
                x.WorkshopId == workshopId &&
                x.IsActive);

            if (customer == null)
                return ServiceResult<int>.Fail("Müşteri bulunamadı.");
            
            var oldValues = new
            {
                customer.Type,
                DisplayName = GetCustomerDisplayName(customer)
            };

            customer.Type = request.Type;

            customer.PhoneNumber = request.PhoneNumber!.Trim();

            customer.FullName = request.FullName?.Trim();
            customer.CompanyName = request.CompanyName?.Trim();
            customer.AuthorizedPersonName = request.AuthorizedPersonName?.Trim();

            customer.Email = request.Email?.Trim();

            customer.TaxOffice = request.TaxOffice?.Trim();
            customer.TaxNumber = request.TaxNumber?.Trim();
            customer.NationalIdentityNumber = request.NationalIdentityNumber?.Trim();

            customer.Address = request.Address?.Trim();
            customer.AddressCity = request.AddressCity?.Trim();
            customer.AddressDistrict = request.AddressDistrict?.Trim();

            await auditLogService.AddAsync(new AuditLogCreateDto
            {
                WorkshopId = workshopId,
                ActionType = AuditActionType.Update,
                EntityType = AuditEntityType.Customer,
                EntityId = customer.Id,
                Description = $"Müşteri güncellendi: {GetCustomerDisplayName(customer)}",
                OldValues = oldValues,
                NewValues = new
                {
                    customer.Type,
                    DisplayName = GetCustomerDisplayName(customer)
                }
            });

            await context.SaveChangesAsync();

            return ServiceResult<int>.Success(customer.Id);
        }

        public async Task<ServiceResult<int>> SetPassiveAsync(int id, int workshopId)
        {
            var customer = await context.Customers
                .FirstOrDefaultAsync(x => x.Id == id && x.WorkshopId == workshopId);

            if (customer == null)
                return ServiceResult<int>.Fail("Müşteri bulunamadı.");

            if (!customer.IsActive)
                return ServiceResult<int>.Fail("Müşteri zaten pasif durumda.");

            customer.IsActive = false;

            await auditLogService.AddAsync(new AuditLogCreateDto
            {
                WorkshopId = workshopId,
                ActionType = AuditActionType.SetPassive,
                EntityType = AuditEntityType.Customer,
                EntityId = customer.Id,
                Description = $"Müşteri pasife alındı: {GetCustomerDisplayName(customer)}",
                OldValues = new
                {
                    IsActive = true
                },
                NewValues = new
                {
                    IsActive = false
                }
            });

            await context.SaveChangesAsync();

            return ServiceResult<int>.Success(customer.Id);
        }

        private static string GetCustomerDisplayName(Customer customer)
        {
            if (!string.IsNullOrWhiteSpace(customer.FullName))
                return customer.FullName;

            if (!string.IsNullOrWhiteSpace(customer.CompanyName))
                return customer.CompanyName;

            return $"Müşteri #{customer.Id}";
        }
    }
}
