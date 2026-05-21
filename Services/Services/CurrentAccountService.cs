
using AutoStock.Repositories;
using AutoStock.Repositories.Entities;
using AutoStock.Repositories.Enums;
using AutoStock.Services.Dtos.Common;
using AutoStock.Services.Dtos.CurrentAccounts;
using AutoStock.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AutoStock.Services.Services
{
    public class CurrentAccountService : ICurrentAccountService
    {
        private readonly AppDbContext _context;

        public CurrentAccountService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ServiceResult<bool>> CreatePaymentAsync(CreatePaymentRequestDto request,int workshopId)
        {
            if (request.Amount <= 0)
                return ServiceResult<bool>.Fail(
                    "Tahsilat tutarı 0'dan büyük olmalıdır.");

            var customerExists = await _context.Customers
                .AnyAsync(x =>
                    x.Id == request.CustomerId &&
                    x.WorkshopId == workshopId);

            if (!customerExists)
                return ServiceResult<bool>.Fail("Müşteri bulunamadı.");

            var transaction = new CurrentAccountTransaction
            {
                WorkshopId = workshopId,
                CustomerId = request.CustomerId,

                Type = CurrentAccountTransactionType.Payment,

                Debit = 0,
                Credit = request.Amount,

                TransactionDate = request.PaymentDate ?? DateTime.UtcNow,

                Description = string.IsNullOrWhiteSpace(request.Description)
                    ? "Tahsilat"
                    : request.Description.Trim(),

                IsSystemGenerated = false
            };

            _context.CurrentAccountTransactions.Add(transaction);

            await _context.SaveChangesAsync();

            return ServiceResult<bool>.Success(true);
        }

        public async Task<ServiceResult<GetCustomerCurrentAccountResponseDto>> GetCustomerAccountAsync(int customerId, int workshopId)
        {
            var customer = await _context.Customers
                .Where(x => x.Id == customerId && x.WorkshopId == workshopId)
                .Select(x => new
                {
                    x.Id,
                    x.FullName
                })
                .FirstOrDefaultAsync();

            if (customer is null)
                return ServiceResult<GetCustomerCurrentAccountResponseDto>.Fail("Müşteri bulunamadı.");

            var transactions = await _context.CurrentAccountTransactions
                .Where(x => x.CustomerId == customerId && x.WorkshopId == workshopId)
                .OrderBy(x => x.TransactionDate)
                .ThenBy(x => x.Id)
                .ToListAsync();

            decimal runningBalance = 0;

            var transactionDtos = transactions
                .Select(x =>
                {
                    runningBalance += x.Debit - x.Credit;

                    return new CurrentAccountTransactionDto
                    {
                        Id = x.Id,
                        TransactionDate = x.TransactionDate,
                        Type = (int)x.Type,
                        TypeText = x.Type.ToString(),
                        Description = x.Description,
                        DocumentNumber = x.DocumentNumber,
                        Debit = x.Debit,
                        Credit = x.Credit,
                        Balance = runningBalance,
                        IsSystemGenerated = x.IsSystemGenerated
                    };
                })
                .ToList();

            var response = new GetCustomerCurrentAccountResponseDto
            {
                CustomerId = customer.Id,
                CustomerName = customer.FullName,
                Balance = runningBalance,
                Transactions = transactionDtos
            };

            return ServiceResult<GetCustomerCurrentAccountResponseDto>.Success(response);
        }
        public async Task<ServiceResult<CurrentAccountSummaryDto>> GetSummaryAsync(int workshopId)
        {
            var now = DateTime.UtcNow;

            var monthStart = new DateTime(now.Year, now.Month, 1);
            var nextMonthStart = monthStart.AddMonths(1);

            var thisMonthInvoiceTotal = await _context.CurrentAccountTransactions
    .Where(x =>
        x.WorkshopId == workshopId &&
        x.TransactionDate >= monthStart &&
        x.TransactionDate < nextMonthStart &&
        (
            x.Type == CurrentAccountTransactionType.InvoiceDebit ||
            x.Type == CurrentAccountTransactionType.Cancel
        ))
    .SumAsync(x => x.Debit - x.Credit);

            var thisMonthPaymentTotal = await _context.CurrentAccountTransactions
                .Where(x =>
                    x.WorkshopId == workshopId &&
                    x.Type == CurrentAccountTransactionType.Payment &&
                    x.TransactionDate >= monthStart &&
                    x.TransactionDate < nextMonthStart)
                .SumAsync(x => x.Credit);

            var customerBalances = await _context.CurrentAccountTransactions
                .Where(x => x.WorkshopId == workshopId)
                .GroupBy(x => new
                {
                    x.CustomerId,
                    x.Customer.FullName
                })
                .Select(g => new CustomerBalanceSummaryDto
                {
                    CustomerId = g.Key.CustomerId,
                    CustomerName = g.Key.FullName,
                    Balance = g.Sum(x => x.Debit - x.Credit)
                })
                .Where(x => x.Balance != 0)
                .OrderByDescending(x => x.Balance)
                .ToListAsync();

            var totalReceivableBalance = customerBalances
                .Where(x => x.Balance > 0)
                .Sum(x => x.Balance);

            var response = new CurrentAccountSummaryDto
            {
                ThisMonthInvoiceTotal = thisMonthInvoiceTotal,
                ThisMonthPaymentTotal = thisMonthPaymentTotal,
                TotalReceivableBalance = totalReceivableBalance,
                CustomerBalances = customerBalances
            };

            return ServiceResult<CurrentAccountSummaryDto>.Success(response);
        }
    }
}