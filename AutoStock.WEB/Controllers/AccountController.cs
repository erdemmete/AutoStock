using AutoStock.WEB.Models.Account;
using AutoStock.WEB.Services;
using Microsoft.AspNetCore.Mvc;

namespace AutoStock.WEB.Controllers
{
    public class AccountController : BaseController
    {
        private readonly AccountApiService _accountApiService;

        public AccountController(AccountApiService accountApiService)
        {
            _accountApiService = accountApiService;
        }

        [HttpGet("/Account")]
        public async Task<IActionResult> Index()
        {
            if (!IsOwner && !IsStaff)
                return RedirectByRole();

            var model = await BuildPageModelAsync();

            if (model is null)
                return RedirectToLogin();

            return View(model);
        }

        [HttpPost("/Account/Email")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateEmail(AccountEmailFormViewModel model)
        {
            if (!IsOwner && !IsStaff)
                return RedirectByRole();

            if (!ModelState.IsValid)
            {
                if (IsAjaxOrJsonRequest())
                    return BadRequest(BuildJsonError());

                var pageModel = await BuildPageModelAsync(model);
                return View("Index", pageModel);
            }

            var result = await _accountApiService.UpdateEmailAsync(model);

            if (result.IsFailure)
            {
                if (IsAjaxOrJsonRequest())
                    return BadRequest(BuildJsonError(result.ErrorMessage ?? "E-posta bilgisi güncellenemedi."));

                ShowError(result.ErrorMessage ?? "E-posta bilgisi güncellenemedi.");
                var pageModel = await BuildPageModelAsync(model);
                return View("Index", pageModel);
            }

            var successMessage = string.IsNullOrWhiteSpace(model.Email)
                ? "E-posta adresiniz kaldırıldı."
                : "E-posta adresiniz güncellendi.";

            if (IsAjaxOrJsonRequest())
            {
                return Json(new
                {
                    isSuccess = true,
                    message = successMessage,
                    email = model.Email?.Trim(),
                    emailSummary = string.IsNullOrWhiteSpace(model.Email)
                        ? "E-posta adresi eklenmemiş"
                        : model.Email.Trim()
                });
            }

            ShowSuccess(string.IsNullOrWhiteSpace(model.Email)
                ? "E-posta adresiniz kaldırıldı."
                : "E-posta adresiniz güncellendi.");

            return RedirectToAction(nameof(Index));
        }

        [HttpPost("/Account/Password")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordFormViewModel model)
        {
            if (!IsOwner && !IsStaff)
                return RedirectByRole();

            if (!ModelState.IsValid)
            {
                if (IsAjaxOrJsonRequest())
                    return BadRequest(BuildJsonError());

                var pageModel = await BuildPageModelAsync(passwordForm: model);
                return View("Index", pageModel);
            }

            var result = await _accountApiService.ChangePasswordAsync(model);

            if (result.IsFailure)
            {
                if (IsAjaxOrJsonRequest())
                    return BadRequest(BuildJsonError(result.ErrorMessage ?? "Şifre değiştirilemedi."));

                ShowError(result.ErrorMessage ?? "Şifre değiştirilemedi.");
                var pageModel = await BuildPageModelAsync(passwordForm: model);
                return View("Index", pageModel);
            }

            HttpContext.Session.Clear();

            if (IsAjaxOrJsonRequest())
            {
                TempData["ToastSuccess"] = "Şifreniz başarıyla değiştirildi. Lütfen yeniden giriş yapın.";
                return Json(new
                {
                    isSuccess = true,
                    redirectUrl = Url.Action("Login", "Auth")
                });
            }

            TempData["ToastSuccess"] = "Şifreniz başarıyla değiştirildi. Lütfen yeniden giriş yapın.";

            return RedirectToAction("Login", "Auth");
        }

        [HttpPost("/Account/Phone")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePhone(AccountPhoneFormViewModel model)
        {
            if (!IsOwner && !IsStaff)
                return RedirectByRole();

            if (!ModelState.IsValid)
            {
                if (IsAjaxOrJsonRequest())
                    return BadRequest(BuildJsonError());

                var pageModel = await BuildPageModelAsync(phoneForm: model);
                return View("Index", pageModel);
            }

            var result = await _accountApiService.UpdatePhoneAsync(model);

            if (result.IsFailure)
            {
                if (IsAjaxOrJsonRequest())
                    return BadRequest(BuildJsonError(result.ErrorMessage ?? "Telefon numarası güncellenemedi."));

                ShowError(result.ErrorMessage ?? "Telefon numarası güncellenemedi.");
                var pageModel = await BuildPageModelAsync(phoneForm: model);
                return View("Index", pageModel);
            }

            var normalizedPhone = NormalizePhoneForDisplay(model.PhoneNumber);
            var successMessage = string.IsNullOrWhiteSpace(normalizedPhone)
                ? "Telefon numaranız kaldırıldı."
                : "Telefon numaranız güncellendi.";

            if (IsAjaxOrJsonRequest())
            {
                return Json(new
                {
                    isSuccess = true,
                    message = successMessage,
                    phone = normalizedPhone,
                    phoneSummary = string.IsNullOrWhiteSpace(normalizedPhone)
                        ? "Telefon numarası eklenmemiş"
                        : normalizedPhone
                });
            }

            ShowSuccess(successMessage);

            return RedirectToAction(nameof(Index));
        }

        [HttpPost("/Account/Workshop")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateWorkshop(WorkshopProfileFormViewModel model)
        {
            if (!IsOwner)
                return RedirectByRole();

            if (!ModelState.IsValid)
            {
                if (IsAjaxOrJsonRequest())
                    return BadRequest(BuildJsonError());

                var pageModel = await BuildPageModelAsync(workshopProfile: model);
                return View("Index", pageModel);
            }

            var result = await _accountApiService.UpdateWorkshopProfileAsync(model);

            if (result.IsFailure)
            {
                if (IsAjaxOrJsonRequest())
                    return BadRequest(BuildJsonError(result.ErrorMessage ?? "Servis bilgileri güncellenemedi."));

                ShowError(result.ErrorMessage ?? "Servis bilgileri güncellenemedi.");
                var pageModel = await BuildPageModelAsync(workshopProfile: model);
                return View("Index", pageModel);
            }

            if (IsAjaxOrJsonRequest())
            {
                return Json(new
                {
                    isSuccess = true,
                    message = "Servis bilgileri güncellendi.",
                    serviceSummary = BuildWorkshopSummary(model)
                });
            }

            ShowSuccess("Servis bilgileri güncellendi.");

            return RedirectToAction(nameof(Index));
        }

        [HttpPost("/Account/Workshop/BankAccounts/Add")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddWorkshopBankAccount(CreateAccountWorkshopBankAccountViewModel model)
        {
            if (!IsOwner)
                return RedirectByRole();

            if (!ModelState.IsValid)
            {
                if (IsAjaxOrJsonRequest())
                    return BadRequest(BuildJsonError());

                return RedirectToAction(nameof(Index));
            }

            var result = await _accountApiService.CreateWorkshopBankAccountAsync(model);

            if (result.IsFailure)
            {
                if (IsAjaxOrJsonRequest())
                    return BadRequest(BuildJsonError(result.ErrorMessage ?? "Banka hesabı eklenemedi."));

                ShowError(result.ErrorMessage ?? "Banka hesabı eklenemedi.");
                return RedirectToAction(nameof(Index));
            }

            if (IsAjaxOrJsonRequest())
            {
                return Json(new
                {
                    isSuccess = true,
                    message = "Banka hesabı eklendi.",
                    reload = true
                });
            }

            ShowSuccess("Banka hesabı eklendi.");
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("/Account/Workshop/BankAccounts/Update")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateWorkshopBankAccount(UpdateAccountWorkshopBankAccountViewModel model)
        {
            if (!IsOwner)
                return RedirectByRole();

            if (!ModelState.IsValid)
            {
                if (IsAjaxOrJsonRequest())
                    return BadRequest(BuildJsonError());

                return RedirectToAction(nameof(Index));
            }

            var result = await _accountApiService.UpdateWorkshopBankAccountAsync(model);

            if (result.IsFailure)
            {
                if (IsAjaxOrJsonRequest())
                    return BadRequest(BuildJsonError(result.ErrorMessage ?? "Banka hesabı güncellenemedi."));

                ShowError(result.ErrorMessage ?? "Banka hesabı güncellenemedi.");
                return RedirectToAction(nameof(Index));
            }

            if (IsAjaxOrJsonRequest())
            {
                return Json(new
                {
                    isSuccess = true,
                    message = "Banka hesabı güncellendi.",
                    reload = true
                });
            }

            ShowSuccess("Banka hesabı güncellendi.");
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("/Account/Workshop/BankAccounts/Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteWorkshopBankAccount(int bankAccountId)
        {
            if (!IsOwner)
                return RedirectByRole();

            var result = await _accountApiService.DeleteWorkshopBankAccountAsync(bankAccountId);

            if (result.IsFailure)
            {
                if (IsAjaxOrJsonRequest())
                    return BadRequest(BuildJsonError(result.ErrorMessage ?? "Banka hesabı silinemedi."));

                ShowError(result.ErrorMessage ?? "Banka hesabı silinemedi.");
                return RedirectToAction(nameof(Index));
            }

            if (IsAjaxOrJsonRequest())
            {
                return Json(new
                {
                    isSuccess = true,
                    message = "Banka hesabı kaldırıldı.",
                    reload = true
                });
            }

            ShowSuccess("Banka hesabı kaldırıldı.");
            return RedirectToAction(nameof(Index));
        }

        private async Task<AccountPageViewModel?> BuildPageModelAsync(
            AccountEmailFormViewModel? emailForm = null,
            AccountPhoneFormViewModel? phoneForm = null,
            ChangePasswordFormViewModel? passwordForm = null,
            WorkshopProfileFormViewModel? workshopProfile = null)
        {
            var overviewResult = await _accountApiService.GetOverviewAsync();

            if (overviewResult.IsFailure || overviewResult.Data is null)
            {
                ShowError(overviewResult.ErrorMessage ?? "Hesap bilgileri alınamadı.");
                return null;
            }

            var pageModel = new AccountPageViewModel
            {
                Overview = overviewResult.Data,
                EmailForm = emailForm ?? new AccountEmailFormViewModel
                {
                    Email = overviewResult.Data.Email
                },
                PhoneForm = phoneForm ?? new AccountPhoneFormViewModel
                {
                    PhoneNumber = overviewResult.Data.PhoneNumber
                },
                PasswordForm = passwordForm ?? new ChangePasswordFormViewModel(),
                IsOwner = IsOwner
            };

            if (IsOwner)
            {
                if (workshopProfile is not null)
                {
                    pageModel.WorkshopProfile = workshopProfile;
                }
                else
                {
                    var profileResult = await _accountApiService.GetWorkshopProfileAsync();
                    pageModel.WorkshopProfile = profileResult.Data ?? new WorkshopProfileFormViewModel
                    {
                        WorkshopName = overviewResult.Data.WorkshopName
                    };
                }

                var membershipResult = await _accountApiService.GetMembershipAsync();
                pageModel.Membership = membershipResult.Data;
            }

            return pageModel;
        }

        private object BuildJsonError(string? fallback = null)
        {
            var errors = ModelState
                .Where(x => x.Value is not null)
                .SelectMany(x => x.Value!.Errors)
                .Select(x => x.ErrorMessage)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

            if (!errors.Any() && !string.IsNullOrWhiteSpace(fallback))
                errors.Add(fallback);

            if (!errors.Any())
                errors.Add("İşlem tamamlanamadı.");

            return new
            {
                isSuccess = false,
                errorMessage = errors.First(),
                errorMessages = errors
            };
        }

        private bool IsAjaxOrJsonRequest()
        {
            var requestedWith = Request.Headers["X-Requested-With"].ToString();
            var accept = Request.Headers.Accept.ToString();

            return string.Equals(requestedWith, "XMLHttpRequest", StringComparison.OrdinalIgnoreCase) ||
                   accept.Contains("application/json", StringComparison.OrdinalIgnoreCase);
        }

        private static string BuildWorkshopSummary(WorkshopProfileFormViewModel model)
        {
            var name = !string.IsNullOrWhiteSpace(model.DisplayName)
                ? model.DisplayName.Trim()
                : "Servis bilgileri";

            var location = string.Join(" / ", new[]
                {
                    model.District,
                    model.City
                }
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!.Trim()));

            return string.IsNullOrWhiteSpace(location)
                ? name
                : $"{name} · {location}";
        }

        private static string? NormalizePhoneForDisplay(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var digits = new string(value.Where(char.IsDigit).ToArray());

            if (digits.StartsWith("90", StringComparison.Ordinal) && digits.Length == 12)
                digits = digits[2..];

            if (digits.StartsWith("5", StringComparison.Ordinal) && digits.Length == 10)
                digits = $"0{digits}";

            if (digits.Length != 11)
                return value.Trim();

            return $"{digits[..4]} {digits.Substring(4, 3)} {digits.Substring(7, 2)} {digits.Substring(9, 2)}";
        }
    }
}
