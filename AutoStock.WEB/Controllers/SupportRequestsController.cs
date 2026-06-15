using AutoStock.WEB.Models.SupportRequests;
using AutoStock.WEB.Services;
using Microsoft.AspNetCore.Mvc;

namespace AutoStock.WEB.Controllers
{
    public class SupportRequestsController : BaseController
    {
        private readonly SupportRequestApiService _supportRequestApiService;

        public SupportRequestsController(SupportRequestApiService supportRequestApiService)
        {
            _supportRequestApiService = supportRequestApiService;
        }

        public async Task<IActionResult> Index(SupportRequestListQueryViewModel query)
        {
            if (!IsOwner && !IsStaff)
                return RedirectToLogin();

            query ??= new SupportRequestListQueryViewModel();

            var result = await _supportRequestApiService.GetPagedAsync(query);

            if (result.IsFailure || result.Data == null)
            {
                ShowError(result.ErrorMessage ?? "Destek talepleri alınırken hata oluştu.");

                return View(new SupportRequestIndexViewModel
                {
                    Query = query,
                    IsOwner = IsOwner
                });
            }

            return View(new SupportRequestIndexViewModel
            {
                Requests = result.Data,
                Query = query,
                IsOwner = IsOwner
            });
        }

        public async Task<IActionResult> Detail(int id)
        {
            if (!IsOwner && !IsStaff)
                return RedirectToLogin();

            var result = await _supportRequestApiService.GetByIdAsync(id);

            return ViewObjectResult(
                result,
                "Destek talebi detayı alınırken hata oluştu.",
                () => RedirectToAction(nameof(Index)));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMessage(CreateSupportRequestMessageViewModel model)
        {
            if (!IsOwner && !IsStaff)
                return RedirectToLogin();

            var result = await _supportRequestApiService.AddMessageAsync(model);

            return HandleCommandResult(
                result,
                onSuccess: () => RedirectToAction(nameof(Detail), new { id = model.Id }),
                onFailure: () => RedirectToAction(nameof(Detail), new { id = model.Id }),
                defaultErrorMessage: "Mesaj eklenirken hata oluştu.",
                successMessage: "Mesajınız gönderildi.");
        }

        [HttpGet]
        public IActionResult CreateIssue()
        {
            if (!IsOwner && !IsStaff)
                return RedirectToLogin();

            return View(new CreateIssueSupportRequestViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateIssue(CreateIssueSupportRequestViewModel model)
        {
            if (!IsOwner && !IsStaff)
                return RedirectToLogin();

            var result = await _supportRequestApiService.CreateIssueAsync(model);

            return HandleCommandResult(
                result,
                onSuccess: () => RedirectToAction(nameof(Detail), new { id = result.Data }),
                onFailure: () => View(model),
                defaultErrorMessage: "Destek talebi oluşturulurken hata oluştu.",
                successMessage: "Destek talebiniz oluşturuldu.");
        }

        [HttpGet]
        public IActionResult CreateUserRequest()
        {
            if (!IsOwner)
            {
                ShowError("Kullanıcı ekleme talebini sadece servis sahibi oluşturabilir.");
                return RedirectToAction(nameof(Index));
            }

            return View(new CreateUserSupportRequestViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUserRequest(CreateUserSupportRequestViewModel model)
        {
            if (!IsOwner)
            {
                ShowError("Kullanıcı ekleme talebini sadece servis sahibi oluşturabilir.");
                return RedirectToAction(nameof(Index));
            }

            var result = await _supportRequestApiService.CreateUserRequestAsync(model);

            return HandleCommandResult(
                result,
                onSuccess: () => RedirectToAction(nameof(Detail), new { id = result.Data }),
                onFailure: () => View(model),
                defaultErrorMessage: "Kullanıcı ekleme talebi oluşturulurken hata oluştu.",
                successMessage: "Kullanıcı ekleme talebiniz oluşturuldu.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            if (!IsOwner && !IsStaff)
                return RedirectToLogin();

            var result = await _supportRequestApiService.CancelAsync(id);

            return HandleCommandResult(
                result,
                onSuccess: () => RedirectToAction(nameof(Index)),
                onFailure: () => RedirectToAction(nameof(Detail), new { id }),
                defaultErrorMessage: "Destek talebi iptal edilirken hata oluştu.",
                successMessage: "Destek talebi iptal edildi.");
        }
    }
}
