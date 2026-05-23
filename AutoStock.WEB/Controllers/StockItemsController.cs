using AutoStock.WEB.Models.StockItems;
using AutoStock.WEB.Services;
using Microsoft.AspNetCore.Mvc;

namespace AutoStock.WEB.Controllers
{
    public class StockItemsController : Controller
    {
        private readonly StockItemApiService _stockItemApiService;

        public StockItemsController(StockItemApiService stockItemApiService)
        {
            _stockItemApiService = stockItemApiService;
        }

        [HttpGet("StockItems")]
        public async Task<IActionResult> Index()
        {
            var result = await _stockItemApiService.GetListAsync();

            return View(result);
        }

        [HttpGet("StockItems/Create")]
        public IActionResult Create()
        {
            return View(new CreateStockItemViewModel());
        }

        [HttpPost("StockItems/Create")]
        public async Task<IActionResult> Create(CreateStockItemViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Name))
                ModelState.AddModelError(nameof(model.Name), "Stok adı zorunludur.");

            if (string.IsNullOrWhiteSpace(model.Unit))
                ModelState.AddModelError(nameof(model.Unit), "Birim zorunludur.");

            if (!ModelState.IsValid)
            {
                TempData["ToastError"] = "Lütfen zorunlu alanları kontrol edin.";
                return View(model);
            }

            var isSuccess = await _stockItemApiService.CreateAsync(model);

            if (!isSuccess)
            {
                TempData["ToastError"] = "Stok kartı oluşturulurken hata oluştu.";
                return View(model);
            }

            TempData["ToastSuccess"] = "Stok kartı başarıyla oluşturuldu.";

            return RedirectToAction(nameof(Index));
        }
    }
}