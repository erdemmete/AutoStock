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
    }
}