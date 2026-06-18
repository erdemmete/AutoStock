using AutoStock.WEB.Models.Common;
using AutoStock.WEB.Models.StockItems;
using AutoStock.WEB.Services;
using Microsoft.AspNetCore.Mvc;

namespace AutoStock.WEB.Controllers
{
    public class StockItemsController : BaseController
    {
        private readonly StockItemApiService _stockItemApiService;
        private readonly StockItemPageService _stockItemPageService;

        public StockItemsController(
    StockItemApiService stockItemApiService,
    StockItemPageService stockItemPageService)
        {
            _stockItemApiService = stockItemApiService;
            _stockItemPageService = stockItemPageService;
        }

        [HttpGet("StockItems")]
        public async Task<IActionResult> Index([FromQuery] StockItemListQueryViewModel query)
        {
            var pageResult = await _stockItemPageService.BuildIndexAsync(query);

            if (pageResult.HasErrors)
            {
                ShowErrors(pageResult.ErrorMessages);
            }

            return View(pageResult.ViewModel);
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

            var result = await _stockItemApiService.CreateAsync(model);

            return HandleCommandResult(
                result,
                onSuccess: () => RedirectToAction(nameof(Index)),
                onFailure: () => View(model),
                defaultErrorMessage: "Stok kartı oluşturulurken hata oluştu.",
                successMessage: "Stok kartı başarıyla oluşturuldu.");
        }

        [HttpGet("StockItems/Details/{id:int}")]
        public async Task<IActionResult> Details(int id)
        {
            var result = await _stockItemApiService.GetByIdAsync(id);

            return ViewObjectResult(
                result,
                "Stok kartı görüntülenirken hata oluştu.",
                onFailure: () => RedirectToAction(nameof(Index)));
        }

        [HttpPost("StockItems/AdjustStock/{id:int}")]
        public async Task<IActionResult> AdjustStock(int id, AdjustStockViewModel model)
        {
            var ownerAccess = RequireOwnerAccess();
            if (ownerAccess is not null)
                return ownerAccess;

            if (model.NewQuantity < 0)
            {
                TempData["ToastError"] = "Yeni stok miktarı negatif olamaz.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var result = await _stockItemApiService.AdjustStockAsync(id, model);

            return HandleCommandResult(
                result,
                onSuccess: () => RedirectToAction(nameof(Details), new { id }),
                onFailure: () => RedirectToAction(nameof(Details), new { id }),
                defaultErrorMessage: "Stok düzeltme işlemi başarısız oldu.",
                successMessage: "Stok miktarı başarıyla güncellendi.");
        }

        [HttpPost("StockItems/StockIn/{id:int}")]
        public async Task<IActionResult> StockIn(int id, StockTransactionViewModel model)
        {
            if (model.Quantity <= 0)
            {
                TempData["ToastError"] = "Stok giriş miktarı sıfırdan büyük olmalıdır.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (!model.UnitPrice.HasValue || model.UnitPrice.Value <= 0)
            {
                TempData["ToastError"] = "Geçerli bir birim alış fiyatı giriniz.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var result = await _stockItemApiService.StockInAsync(id, model);

            return HandleCommandResult(
                result,
                onSuccess: () => RedirectToAction(nameof(Details), new { id }),
                onFailure: () => RedirectToAction(nameof(Details), new { id }),
                defaultErrorMessage: "Stok girişi yapılırken hata oluştu.",
                successMessage: "Stok girişi başarıyla kaydedildi.");
        }

        [HttpGet("StockItems/Edit/{id:int}")]
        public async Task<IActionResult> Edit(int id)
        {
            var ownerAccess = RequireOwnerAccess();
            if (ownerAccess is not null)
                return ownerAccess;

            var result = await _stockItemApiService.GetEditModelAsync(id);

            return ViewObjectResult(
                result,
                "Stok kartı düzenleme bilgileri alınırken hata oluştu.",
                onFailure: () => RedirectToAction(nameof(Index)));
        }

        [HttpPost("StockItems/Edit/{id:int}")]
        public async Task<IActionResult> Edit(int id, EditStockItemViewModel model)
        {
            var ownerAccess = RequireOwnerAccess();
            if (ownerAccess is not null)
                return ownerAccess;

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

            var result = await _stockItemApiService.UpdateAsync(model);

            return HandleCommandResult(
                result,
                onSuccess: () => RedirectToAction(nameof(Details), new { id = model.Id }),
                onFailure: () => View(model),
                defaultErrorMessage: "Stok kartı güncellenirken hata oluştu.",
                successMessage: "Stok kartı başarıyla güncellendi.");
        }

        [HttpPost("StockItems/Passive/{id:int}")]
        public async Task<IActionResult> SetPassive(int id)
        {
            var ownerAccess = RequireOwnerAccess();
            if (ownerAccess is not null)
                return ownerAccess;

            var result = await _stockItemApiService.SetPassiveAsync(id);

            return HandleCommandResult(
                result,
                onSuccess: () => RedirectToAction(nameof(Index)),
                onFailure: () => RedirectToAction(nameof(Details), new { id }),
                defaultErrorMessage: "Stok kartı silinirken hata oluştu.",
                successMessage: "Stok kartı başarıyla pasifleştirildi.");
        }
    }
}
