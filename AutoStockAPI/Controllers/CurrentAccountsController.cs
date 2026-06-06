
using AutoStock.Services.Dtos.CurrentAccounts;
using AutoStock.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoStock.API.Controllers
{
    [ApiController]
    [Route("api/current-accounts")]
    [Authorize]
    public class CurrentAccountsController : ControllerBase
    {
        private readonly ICurrentAccountService _currentAccountService;

        public CurrentAccountsController(ICurrentAccountService currentAccountService)
        {
            _currentAccountService = currentAccountService;
        }

        [HttpPost("payments")]
        public async Task<IActionResult> CreatePayment(CreatePaymentRequestDto request)
        {
            var workshopIdClaim =
                User.FindFirst("WorkshopId")?.Value
                ?? User.FindFirst("workshopId")?.Value;

            if (!int.TryParse(workshopIdClaim, out var workshopId))
                return Unauthorized("Workshop bilgisi bulunamadı.");

            var result = await _currentAccountService.CreatePaymentAsync(request, workshopId);

            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result);
        }
        [HttpGet("customers/{customerId:int}")]
        public async Task<IActionResult> GetCustomerAccount(int customerId)
        {
            var workshopIdClaim =
                User.FindFirst("WorkshopId")?.Value
                ?? User.FindFirst("workshopId")?.Value;

            if (!int.TryParse(workshopIdClaim, out var workshopId))
                return Unauthorized("Workshop bilgisi bulunamadı.");

            var result = await _currentAccountService.GetCustomerAccountAsync(customerId, workshopId);

            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary([FromQuery] CurrentAccountListQueryDto query)
        {
            var workshopIdClaim =
                User.FindFirst("WorkshopId")?.Value
                ?? User.FindFirst("workshopId")?.Value;

            if (!int.TryParse(workshopIdClaim, out var workshopId))
                return Unauthorized("Workshop bilgisi bulunamadı.");

            var result = await _currentAccountService.GetPagedSummaryAsync(query, workshopId);

            if (result.IsFailure)
                return StatusCode((int)result.StatusCode, result);

            return Ok(result);
        }
    }
}