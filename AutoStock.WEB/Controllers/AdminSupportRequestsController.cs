using AutoStock.WEB.Models.SupportRequests;
using AutoStock.WEB.Services;
using Microsoft.AspNetCore.Mvc;

namespace AutoStock.WEB.Controllers
{
    public class AdminSupportRequestsController : BaseController
    {
        private readonly SupportRequestApiService _supportRequestApiService;

        public AdminSupportRequestsController(SupportRequestApiService supportRequestApiService)
        {
            _supportRequestApiService = supportRequestApiService;
        }

        public async Task<IActionResult> Index(AdminSupportRequestListQueryViewModel query)
        {
            if (!IsAdmin)
                return RedirectToLogin();

            query ??= new AdminSupportRequestListQueryViewModel();

            var result = await _supportRequestApiService.GetPagedForAdminAsync(query);

            if (result.IsFailure || result.Data == null)
            {
                ShowError(result.ErrorMessage ?? "Destek talepleri alınırken hata oluştu.");

                return View(new AdminSupportRequestIndexViewModel
                {
                    Query = query
                });
            }

            return View(new AdminSupportRequestIndexViewModel
            {
                Requests = result.Data,
                Query = query
            });
        }

        public async Task<IActionResult> Detail(int id)
        {
            if (!IsAdmin)
                return RedirectToLogin();

            var result = await _supportRequestApiService.GetByIdForAdminAsync(id);

            return ViewObjectResult(
                result,
                "Destek talebi detayı alınırken hata oluştu.",
                () => RedirectToAction(nameof(Index)));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Answer(AdminAnswerSupportRequestViewModel model)
        {
            if (!IsAdmin)
                return RedirectToLogin();

            var result = await _supportRequestApiService.AnswerAsync(model);

            return HandleCommandResult(
                result,
                onSuccess: () => RedirectToAction(nameof(Detail), new { id = model.Id }),
                onFailure: () => RedirectToAction(nameof(Detail), new { id = model.Id }),
                defaultErrorMessage: "Destek talebi yanıtlanırken hata oluştu.",
                successMessage: "Destek talebi yanıtlandı.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(AdminUpdateSupportRequestStatusViewModel model)
        {
            if (!IsAdmin)
                return RedirectToLogin();

            var result = await _supportRequestApiService.UpdateStatusAsync(model);

            return HandleCommandResult(
                result,
                onSuccess: () => RedirectToAction(nameof(Detail), new { id = model.Id }),
                onFailure: () => RedirectToAction(nameof(Detail), new { id = model.Id }),
                defaultErrorMessage: "Destek talebi durumu güncellenirken hata oluştu.",
                successMessage: "Destek talebi durumu güncellendi.");
        }
    }
}