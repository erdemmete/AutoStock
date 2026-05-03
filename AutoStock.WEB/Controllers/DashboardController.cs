using Microsoft.AspNetCore.Mvc;

namespace AutoStock.WEB.Controllers
{
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            var token = HttpContext.Session.GetString("AuthToken");

            if (string.IsNullOrWhiteSpace(token))
            {
                return RedirectToAction("Login", "Auth");
            }

            return View();
        }
    }
}