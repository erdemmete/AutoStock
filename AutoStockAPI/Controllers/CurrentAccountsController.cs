using AutoStock.Services.Constants;
using AutoStock.Services.Dtos.CurrentAccounts;
using AutoStock.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoStock.API.Controllers
{
    [ApiController]
    [Route("api/current-accounts")]
    [Authorize(Roles = AppRoles.Owner)]
    public class CurrentAccountsController : BaseApiController
    {
        private readonly ICurrentAccountService _currentAccountService;

        public CurrentAccountsController(ICurrentAccountService currentAccountService)
        {
            _currentAccountService = currentAccountService;
        }

        [HttpPost("payments")]
        public async Task<IActionResult> CreatePayment(CreatePaymentRequestDto request)
        {
            var workshopIdResult = GetCurrentWorkshopId();

            if (workshopIdResult.IsFailure)
                return UnauthorizedResult(workshopIdResult);

            var result = await _currentAccountService.CreatePaymentAsync(
                request,
                workshopIdResult.Data);

            return ToActionResult(result);
        }

        [HttpGet("customers/{customerId:int}")]
        public async Task<IActionResult> GetCustomerAccount(int customerId)
        {
            var workshopIdResult = GetCurrentWorkshopId();

            if (workshopIdResult.IsFailure)
                return UnauthorizedResult(workshopIdResult);

            var result = await _currentAccountService.GetCustomerAccountAsync(
                customerId,
                workshopIdResult.Data);

            return ToActionResult(result);
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary([FromQuery] CurrentAccountListQueryDto query)
        {
            var workshopIdResult = GetCurrentWorkshopId();

            if (workshopIdResult.IsFailure)
                return UnauthorizedResult(workshopIdResult);

            var result = await _currentAccountService.GetPagedSummaryAsync(
                query,
                workshopIdResult.Data);

            return ToActionResult(result);
        }
    }
}