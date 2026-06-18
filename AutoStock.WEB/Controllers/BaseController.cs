using AutoStock.WEB.Models.Common;
using Microsoft.AspNetCore.Mvc;

namespace AutoStock.WEB.Controllers
{
    public abstract class BaseController : Controller
    {
        protected IActionResult ViewListResult<T>(
            ApiResponse<List<T>> result,
            string defaultErrorMessage)
        {
            if (result.IsFailure)
            {
                ShowError(result.ErrorMessage ?? defaultErrorMessage);

                return View(new List<T>());
            }

            return View(result.Data ?? new List<T>());
        }

        protected IActionResult HandleCommandResult<T>(
            ApiResponse<T> result,
            Func<IActionResult> onSuccess,
            Func<IActionResult> onFailure,
            string defaultErrorMessage,
            string? successMessage = null)
        {
            if (result.IsFailure)
            {
                ShowError(result.ErrorMessage ?? defaultErrorMessage);

                return onFailure();
            }

            if (!string.IsNullOrWhiteSpace(successMessage))
            {
                ShowSuccess(successMessage);
            }

            return onSuccess();
        }

        protected void ShowSuccess(string message)
        {
            TempData["ToastSuccess"] = message;
        }

        protected void ShowError(string message)
        {
            TempData["ToastError"] = message;
        }

        protected IActionResult ViewObjectResult<T>(ApiResponse<T> result, string defaultErrorMessage, Func<IActionResult>? onFailure = null)
        {
            if (result.IsFailure || result.Data == null)
            {
                ShowError(result.ErrorMessage ?? defaultErrorMessage);

                return onFailure?.Invoke() ?? RedirectToAction("Index");
            }

            return View(result.Data);
        }

        protected void ShowErrors(IEnumerable<string> messages)
        {
            var errorMessages = messages
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            if (!errorMessages.Any())
                return;

            TempData["ToastError"] = string.Join(" ", errorMessages);
        }

        protected string? CurrentUserRole =>
    HttpContext.Session.GetString("UserRole");

        protected int? CurrentUserId =>
            HttpContext.Session.GetInt32("UserId");

        protected int? CurrentWorkshopId =>
            HttpContext.Session.GetInt32("WorkshopId");

        protected bool IsAdmin =>
            CurrentUserRole == "Admin";

        protected bool IsOwner =>
            CurrentUserRole == "Owner";

        protected bool IsStaff =>
            CurrentUserRole == "Staff";

        protected IActionResult RedirectToLogin()
        {
            return RedirectToAction("Login", "Auth");
        }

        protected IActionResult RedirectByRole()
        {
            if (IsAdmin)
                return RedirectToAction("Dashboard", "Admin");

            return RedirectToAction("Index", "Dashboard");
        }

        protected IActionResult? RequireOwnerAccess()
        {
            if (IsOwner)
                return null;

            if (string.IsNullOrWhiteSpace(CurrentUserRole))
                return RedirectToLogin();

            const string message = "Bu alan için servis sahibi yetkisi gerekir.";

            if (IsAjaxOrJsonRequest())
            {
                return StatusCode(403, new
                {
                    isSuccess = false,
                    errorMessage = message,
                    errorMessages = new[] { message }
                });
            }

            ShowError(message);

            return RedirectToAction("Index", "Dashboard");
        }

        private bool IsAjaxOrJsonRequest()
        {
            var requestedWith = Request.Headers["X-Requested-With"].ToString();
            var accept = Request.Headers.Accept.ToString();
            var contentType = Request.ContentType ?? string.Empty;

            return string.Equals(requestedWith, "XMLHttpRequest", StringComparison.OrdinalIgnoreCase) ||
                   accept.Contains("application/json", StringComparison.OrdinalIgnoreCase) ||
                   contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase);
        }
    }
}
