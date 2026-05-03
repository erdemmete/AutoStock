using AutoStock.WEB.Models;
using Microsoft.AspNetCore.Mvc;

namespace AutoStock.WEB.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult Index()
        {
            var fullName = HttpContext.Session.GetString("FullName") ?? "";

            var model = new DashboardViewModel
            {
                FullName = fullName,
                WorkshopName = "Admin Paneli"
            };

            return View(model);
        }
    }
}