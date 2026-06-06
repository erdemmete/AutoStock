using AutoStock.WEB.Models.Admin.Workshops;
using AutoStock.WEB.Services;
using Microsoft.AspNetCore.Mvc;

namespace AutoStock.WEB.Controllers
{
    public class AdminController : BaseController
    {
        private readonly AdminWorkshopApiService _adminWorkshopApiService;

        public AdminController(AdminWorkshopApiService adminWorkshopApiService)
        {
            _adminWorkshopApiService = adminWorkshopApiService;
        }

        [HttpGet("Admin")]
        [HttpGet("Admin/Dashboard")]
        public IActionResult Dashboard()
        {
            if (!IsAdmin())
                return RedirectToLogin();

            return View();
        }

        [HttpGet("Admin/Index")]
        public IActionResult Index()
        {
            return RedirectToAction(nameof(Dashboard));
        }

        [HttpGet("Admin/Workshops")]
        public async Task<IActionResult> Workshops()
        {
            if (!IsAdmin())
                return RedirectToLogin();

            var result = await _adminWorkshopApiService.GetListAsync();

            return ViewListResult(
                result,
                "Servis listesi alınırken hata oluştu.");
        }

        [HttpGet("Admin/Workshops/Create")]
        public IActionResult CreateWorkshop()
        {
            if (!IsAdmin())
                return RedirectToLogin();

            var model = new CreateAdminWorkshopViewModel
            {
                IsActive = true,
                SubscriptionStatus = 1,
                TrialDays = 15,
                FirstUserRole = "Owner"
            };

            return View(model);
        }

        [HttpPost("Admin/Workshops/Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateWorkshop(CreateAdminWorkshopViewModel model)
        {
            if (!IsAdmin())
                return RedirectToLogin();

            if (!ModelState.IsValid)
                return View(model);

            var result = await _adminWorkshopApiService.CreateAsync(model);

            return HandleCommandResult(
                result,
                onSuccess: () => RedirectToAction(nameof(WorkshopDetails), new { id = result.Data }),
                onFailure: () => View(model),
                defaultErrorMessage: "Servis oluşturulurken hata oluştu.",
                successMessage: "Servis başarıyla oluşturuldu.");
        }

        [HttpGet("Admin/Workshops/Details/{id:int}")]
        public async Task<IActionResult> WorkshopDetails(int id)
        {
            if (!IsAdmin())
                return RedirectToLogin();

            var result = await _adminWorkshopApiService.GetByIdAsync(id);

            return ViewObjectResult(
                result,
                "Servis detayı alınırken hata oluştu.",
                () => RedirectToAction(nameof(Workshops)));
        }

        [HttpPost("Admin/Workshops/UpdateSubscription")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateWorkshopSubscription(UpdateAdminWorkshopSubscriptionViewModel model)
        {
            if (!IsAdmin())
                return RedirectToLogin();

            if (model.WorkshopId <= 0)
            {
                ShowError("Geçersiz servis bilgisi.");
                return RedirectToAction(nameof(Workshops));
            }

            var result = await _adminWorkshopApiService.UpdateSubscriptionAsync(model);

            return HandleCommandResult(
                result,
                onSuccess: () => RedirectToAction(nameof(WorkshopDetails), new { id = model.WorkshopId }),
                onFailure: () => RedirectToAction(nameof(WorkshopDetails), new { id = model.WorkshopId }),
                defaultErrorMessage: "Servis abonelik bilgileri güncellenirken hata oluştu.",
                successMessage: "Üyelik bilgileri güncellendi.");
        }

        [HttpPost("Admin/Workshops/UpdateProfile")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateWorkshopProfile(UpdateAdminWorkshopProfileViewModel model)
        {
            if (!IsAdmin())
                return RedirectToLogin();

            if (model.WorkshopId <= 0)
            {
                ShowError("Geçersiz servis bilgisi.");
                return RedirectToAction(nameof(Workshops));
            }

            var result = await _adminWorkshopApiService.UpdateProfileAsync(model);

            return HandleCommandResult(
                result,
                onSuccess: () => RedirectToAction(nameof(WorkshopDetails), new { id = model.WorkshopId }),
                onFailure: () => RedirectToAction(nameof(WorkshopDetails), new { id = model.WorkshopId }),
                defaultErrorMessage: "İşletme bilgileri güncellenirken hata oluştu.",
                successMessage: "İşletme bilgileri güncellendi.");
        }

        [HttpPost("Admin/Workshops/AddPartner")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddWorkshopPartner(CreateAdminWorkshopPartnerViewModel model)
        {
            if (!IsAdmin())
                return RedirectToLogin();

            if (model.WorkshopId <= 0)
            {
                ShowError("Geçersiz servis bilgisi.");
                return RedirectToAction(nameof(Workshops));
            }

            if (string.IsNullOrWhiteSpace(model.FullName))
            {
                ShowError("Yetkili/ortak adı zorunludur.");
                return RedirectToAction(nameof(WorkshopDetails), new { id = model.WorkshopId });
            }

            var result = await _adminWorkshopApiService.CreatePartnerAsync(model);

            return HandleCommandResult(
                result,
                onSuccess: () => RedirectToAction(nameof(WorkshopDetails), new { id = model.WorkshopId }),
                onFailure: () => RedirectToAction(nameof(WorkshopDetails), new { id = model.WorkshopId }),
                defaultErrorMessage: "Yetkili/ortak eklenirken hata oluştu.",
                successMessage: "Yetkili/ortak başarıyla eklendi.");
        }

        [HttpPost("Admin/Workshops/DeletePartner")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteWorkshopPartner(int workshopId, int partnerId)
        {
            if (!IsAdmin())
                return RedirectToLogin();

            if (workshopId <= 0 || partnerId <= 0)
            {
                ShowError("Geçersiz yetkili/ortak bilgisi.");
                return RedirectToAction(nameof(Workshops));
            }

            var result = await _adminWorkshopApiService.DeletePartnerAsync(workshopId, partnerId);

            return HandleCommandResult(
                result,
                onSuccess: () => RedirectToAction(nameof(WorkshopDetails), new { id = workshopId }),
                onFailure: () => RedirectToAction(nameof(WorkshopDetails), new { id = workshopId }),
                defaultErrorMessage: "Yetkili/ortak silinirken hata oluştu.",
                successMessage: "Yetkili/ortak silindi.");
        }

        [HttpGet("Admin/Workshops/{workshopId:int}/Users/Create")]
        public async Task<IActionResult> CreateWorkshopUser(int workshopId)
        {
            if (!IsAdmin())
                return RedirectToLogin();

            var result = await _adminWorkshopApiService.GetByIdAsync(workshopId);

            if (result.IsFailure || result.Data == null)
            {
                ShowError(result.ErrorMessage ?? "Servis bulunamadı.");
                return RedirectToAction(nameof(Workshops));
            }

            var model = new CreateAdminWorkshopUserViewModel
            {
                WorkshopId = workshopId,
                Role = "Staff"
            };

            ViewBag.WorkshopName = result.Data.Name;

            return View(model);
        }

        [HttpPost("Admin/Workshops/{workshopId:int}/Users/Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateWorkshopUser(int workshopId, CreateAdminWorkshopUserViewModel model)
        {
            if (!IsAdmin())
                return RedirectToLogin();

            model.WorkshopId = workshopId;

            if (!ModelState.IsValid)
            {
                await PrepareWorkshopUserCreateViewAsync(workshopId);
                return View(model);
            }

            var result = await _adminWorkshopApiService.CreateUserAsync(model);

            if (result.IsFailure)
            {
                await PrepareWorkshopUserCreateViewAsync(workshopId);
                ShowError(result.ErrorMessage ?? "Kullanıcı oluşturulurken hata oluştu.");
                return View(model);
            }

            ShowSuccess("Kullanıcı başarıyla oluşturuldu.");

            TempData["CreatedUserFullName"] = model.FullName;
            TempData["CreatedUserName"] = model.UserName;
            TempData["CreatedUserPassword"] = model.Password;
            TempData["CreatedUserPhone"] = model.PhoneNumber;

            return RedirectToAction(nameof(WorkshopDetails), new { id = workshopId });
        }

        [HttpPost("Admin/Workshops/UpdateUserStatus")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateWorkshopUserStatus(int workshopId, int userId, bool isActive)
        {
            if (!IsAdmin())
                return RedirectToLogin();

            if (workshopId <= 0 || userId <= 0)
            {
                ShowError("Geçersiz kullanıcı bilgisi.");
                return RedirectToAction(nameof(Workshops));
            }

            var result = await _adminWorkshopApiService.UpdateUserStatusAsync(
                workshopId,
                userId,
                isActive);

            return HandleCommandResult(
                result,
                onSuccess: () => RedirectToAction(nameof(WorkshopDetails), new { id = workshopId }),
                onFailure: () => RedirectToAction(nameof(WorkshopDetails), new { id = workshopId }),
                defaultErrorMessage: "Kullanıcı durumu güncellenirken hata oluştu.",
                successMessage: isActive
                    ? "Kullanıcı aktifleştirildi."
                    : "Kullanıcı pasifleştirildi.");
        }
        [HttpGet("Admin/Workshops/{workshopId:int}/Users/SuggestCredentials")]
        public async Task<IActionResult> SuggestWorkshopUserCredentials(int workshopId, string fullName)
        {
            if (!IsAdmin())
            {
                return Json(new
                {
                    isSuccess = false,
                    errorMessage = "Yetkisiz işlem."
                });
            }

            var result = await _adminWorkshopApiService.SuggestCredentialsAsync(
                workshopId,
                fullName);

            if (result.IsFailure || result.Data == null)
            {
                return Json(new
                {
                    isSuccess = false,
                    errorMessage = result.ErrorMessage ?? "Kullanıcı adı ve geçici şifre oluşturulamadı."
                });
            }

            return Json(new
            {
                isSuccess = true,
                userName = result.Data.UserName,
                password = result.Data.Password
            });
        }

        private bool IsAdmin()
        {
            var role = HttpContext.Session.GetString("UserRole");

            return role == "Admin";
        }

        private IActionResult RedirectToLogin()
        {
            return RedirectToAction("Login", "Auth");
        }

        private async Task PrepareWorkshopUserCreateViewAsync(int workshopId)
        {
            var result = await _adminWorkshopApiService.GetByIdAsync(workshopId);

            ViewBag.WorkshopName = result.IsSuccess && result.Data != null
                ? result.Data.Name
                : "Servis";
        }
    }
}