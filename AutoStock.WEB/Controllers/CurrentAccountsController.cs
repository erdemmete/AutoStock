using AutoStock.Mobile.Models.CurrentAccounts;
using AutoStock.WEB.Models.CurrentAccounts;
using AutoStock.WEB.Services;
using Microsoft.AspNetCore.Mvc;

namespace AutoStock.WEB.Controllers
{
    public class CurrentAccountsController : BaseController
    {
        private readonly CurrentAccountApiService _currentAccountApiService;
        private readonly CurrentAccountPageService _currentAccountPageService;

        public CurrentAccountsController(
            CurrentAccountApiService currentAccountApiService,
            CurrentAccountPageService currentAccountPageService)
        {
            _currentAccountApiService = currentAccountApiService;
            _currentAccountPageService = currentAccountPageService;
        }

        [HttpGet("CurrentAccounts")]
        public async Task<IActionResult> Index([FromQuery] CurrentAccountListQueryViewModel query)
        {
            var pageResult = await _currentAccountPageService.BuildIndexAsync(query);

            if (pageResult.HasErrors)
            {
                ShowErrors(pageResult.ErrorMessages);
            }

            return View(pageResult.ViewModel);
        }

        [HttpGet("CurrentAccounts/Customer/{customerId:int}")]
        public async Task<IActionResult> Customer(int customerId)
        {
            var result = await _currentAccountApiService.GetCustomerAccountAsync(customerId);

            return ViewObjectResult(
                result,
                "Cari hesap bulunamadı.",
                onFailure: () => RedirectToAction("Index", "Customers"));
        }

        [HttpPost("CurrentAccounts/CreatePayment")]
        public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentViewModel model)
        {
            if (model.Amount <= 0)
            {
                return BadRequest(new
                {
                    isSuccess = false,
                    errorMessages = new[]
                    {
                        "Tahsilat tutarı 0'dan büyük olmalıdır."
                    }
                });
            }

            var result = await _currentAccountApiService.CreatePaymentAsync(model);

            if (result.IsFailure)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPost("CurrentAccounts/CancelPayment")]
        public async Task<IActionResult> CancelPayment([FromBody] CancelPaymentViewModel model)
        {
            if (model.TransactionId <= 0)
            {
                return BadRequest(new
                {
                    isSuccess = false,
                    errorMessages = new[]
                    {
                        "Geçerli bir tahsilat hareketi seçiniz."
                    }
                });
            }

            var result = await _currentAccountApiService.CancelPaymentAsync(model);

            if (result.IsFailure)
                return BadRequest(result);

            return Ok(result);
        }
    }
}
