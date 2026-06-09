using AutoStock.WEB.Models;
using AutoStock.WEB.Services;
using Microsoft.AspNetCore.Mvc;

namespace AutoStock.WEB.Controllers
{
    public class DashboardController : BaseController
    {
        private readonly DashboardApiService _dashboardApiService;

        public DashboardController(DashboardApiService dashboardApiService)
        {
            _dashboardApiService = dashboardApiService;
        }

        public async Task<IActionResult> Index()
        {
            var token = HttpContext.Session.GetString("AuthToken");

            if (string.IsNullOrWhiteSpace(token))
                return RedirectToLogin();

            if (IsAdmin)
                return RedirectToAction("Dashboard", "Admin");

            var result = await _dashboardApiService.GetAsync();

            if (result.IsFailure || result.Data == null)
            {
                ShowError(result.ErrorMessage ?? "Dashboard bilgileri alınamadı.");

                return View(new DashboardViewModel
                {
                    FullName = HttpContext.Session.GetString("FullName") ?? "",
                    Role = HttpContext.Session.GetString("UserRole") ?? "",
                    WorkshopId = HttpContext.Session.GetInt32("WorkshopId") ?? 0,
                    WorkshopName = "Servis Paneli"
                });
            }

            return View(result.Data);
        }
    }
}