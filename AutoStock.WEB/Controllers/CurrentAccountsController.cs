using AutoStock.Mobile.Models.CurrentAccounts;
using AutoStock.WEB.Models.CurrentAccounts;
using AutoStock.WEB.Services;
using Microsoft.AspNetCore.Mvc;

namespace AutoStock.WEB.Controllers
{
    public class CurrentAccountsController : Controller
    {
        private readonly CurrentAccountApiService _currentAccountApiService;

        public CurrentAccountsController(
            CurrentAccountApiService currentAccountApiService)
        {
            _currentAccountApiService = currentAccountApiService;
        }

        [HttpGet("CurrentAccounts/Customer/{customerId:int}")]
        public async Task<IActionResult> Customer(int customerId)
        {
            var model = await _currentAccountApiService
                .GetCustomerAccountAsync(customerId);

            if (model is null)
            {
                TempData["ErrorMessage"] = "Cari hesap bulunamadı.";

                return RedirectToAction("Index", "Customers");
            }

            return View(model);
        }

        [HttpPost("CurrentAccounts/CreatePayment")]
        public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentViewModel model)
        {
            if (model.Amount <= 0)
            {
                return BadRequest(new
                {
                    isSuccess = false,
                    errorMessage = new[]
                    {
                        "Tahsilat tutarı 0'dan büyük olmalıdır."
                    }
                });
            }

            var success = await _currentAccountApiService
                .CreatePaymentAsync(model);

            if (!success)
            {
                return BadRequest(new
                {
                    isSuccess = false,
                    errorMessage = new[]
                    {
                        "Tahsilat kaydedilemedi."
                    }
                });
            }

            return Ok(new
            {
                isSuccess = true
            });
        }

        [HttpGet("CurrentAccounts")]
        public async Task<IActionResult> Index()
        {
            var model = await _currentAccountApiService.GetSummaryAsync();

            if (model is null)
            {
                TempData["ErrorMessage"] = "Cari özet getirilemedi.";
                return RedirectToAction("Index", "Dashboard");
            }

            return View(model);
        }
    }
}