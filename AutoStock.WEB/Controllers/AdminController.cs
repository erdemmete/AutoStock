using AutoStock.WEB.Models.Admin.Workshops;
using AutoStock.WEB.Services;
using Microsoft.AspNetCore.Mvc;

namespace AutoStock.WEB.Controllers
{
    public class AdminController : Controller
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

            var workshops = await _adminWorkshopApiService.GetListAsync();

            return View(workshops);
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

            if (!result.IsSuccess)
            {
                ViewBag.Error = result.ErrorMessage;
                return View(model);
            }

            TempData["SuccessMessage"] = "Servis başarıyla oluşturuldu.";

            return RedirectToAction(nameof(Workshops));
        }

        [HttpGet("Admin/Workshops/Details/{id:int}")]
        public async Task<IActionResult> WorkshopDetails(int id)
        {
            if (!IsAdmin())
                return RedirectToLogin();

            var workshop = await _adminWorkshopApiService.GetByIdAsync(id);

            if (workshop == null)
            {
                TempData["ErrorMessage"] = "Servis bulunamadı.";
                return RedirectToAction(nameof(Workshops));
            }

            return View(workshop);
        }

        [HttpPost("Admin/Workshops/UpdateSubscription")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateWorkshopSubscription(
            UpdateAdminWorkshopSubscriptionViewModel model)
        {
            if (!IsAdmin())
                return RedirectToLogin();

            if (model.WorkshopId <= 0)
            {
                TempData["ErrorMessage"] = "Geçersiz servis bilgisi.";
                return RedirectToAction(nameof(Workshops));
            }

            var result = await _adminWorkshopApiService.UpdateSubscriptionAsync(model);

            if (!result.IsSuccess)
            {
                TempData["ErrorMessage"] = result.ErrorMessage;
                return RedirectToAction(nameof(WorkshopDetails), new { id = model.WorkshopId });
            }

            TempData["SuccessMessage"] = "Üyelik bilgileri güncellendi.";

            return RedirectToAction(nameof(WorkshopDetails), new { id = model.WorkshopId });
        }

        [HttpPost("Admin/Workshops/UpdateProfile")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateWorkshopProfile(
            UpdateAdminWorkshopProfileViewModel model)
        {
            if (!IsAdmin())
                return RedirectToLogin();

            if (model.WorkshopId <= 0)
            {
                TempData["ErrorMessage"] = "Geçersiz servis bilgisi.";
                return RedirectToAction(nameof(Workshops));
            }

            var result = await _adminWorkshopApiService.UpdateProfileAsync(model);

            if (!result.IsSuccess)
            {
                TempData["ErrorMessage"] = result.ErrorMessage;
                return RedirectToAction(nameof(WorkshopDetails), new { id = model.WorkshopId });
            }

            TempData["SuccessMessage"] = "İşletme bilgileri güncellendi.";

            return RedirectToAction(nameof(WorkshopDetails), new { id = model.WorkshopId });
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
    }
}