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

        [HttpGet("StockItems/Details/{id:int}")]
        public async Task<IActionResult> Details(int id)
        {
            var result = await _stockItemApiService.GetByIdAsync(id);

            if (result == null)
            {
                TempData["ToastError"] = "Stok kartı bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            return View(result);
        }

        [HttpPost("StockItems/AdjustStock/{id:int}")]
        public async Task<IActionResult> AdjustStock(int id, AdjustStockViewModel model)
        {
            if (model.NewQuantity < 0)
            {
                TempData["ToastError"] = "Yeni stok miktarı negatif olamaz.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var isSuccess = await _stockItemApiService.AdjustStockAsync(id, model);

            if (!isSuccess)
            {
                TempData["ToastError"] = "Stok düzeltme işlemi başarısız oldu.";
                return RedirectToAction(nameof(Details), new { id });
            }

            TempData["ToastSuccess"] = "Stok miktarı başarıyla güncellendi.";

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpGet("StockItems/Edit/{id:int}")]
        public async Task<IActionResult> Edit(int id)
        {
            var result = await _stockItemApiService.GetEditModelAsync(id);

            if (result == null)
            {
                TempData["ToastError"] = "Stok kartı bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            return View(result);
        }

        [HttpPost("StockItems/Edit/{id:int}")]
        public async Task<IActionResult> Edit(int id, EditStockItemViewModel model)
        {
            if (id != model.Id)
            {
                TempData["ToastError"] = "Stok bilgisi hatalı.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(model.Name))
                ModelState.AddModelError(nameof(model.Name), "Stok adı zorunludur.");

            if (string.IsNullOrWhiteSpace(model.Unit))
                ModelState.AddModelError(nameof(model.Unit), "Birim zorunludur.");

            if (!ModelState.IsValid)
            {
                TempData["ToastError"] = "Lütfen zorunlu alanları kontrol edin.";
                return View(model);
            }

            var isSuccess = await _stockItemApiService.UpdateAsync(model);

            if (!isSuccess)
            {
                TempData["ToastError"] = "Stok kartı güncellenirken hata oluştu.";
                return View(model);
            }

            TempData["ToastSuccess"] = "Stok kartı başarıyla güncellendi.";

            return RedirectToAction(nameof(Details), new { id = model.Id });
        }

        [HttpPost("StockItems/Passive/{id:int}")]
        public async Task<IActionResult> SetPassive(int id)
        {
            var isSuccess = await _stockItemApiService.SetPassiveAsync(id);

            if (!isSuccess)
            {
                TempData["ToastError"] = "Stok kartı silinirken hata oluştu.";
                return RedirectToAction(nameof(Details), new { id });
            }

            TempData["ToastSuccess"] = "Stok kartı başarıyla silindi.";

            return RedirectToAction(nameof(Index));
        }
    }
}