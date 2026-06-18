using AutoStock.WEB.Models.Admin.Workshops;
using AutoStock.WEB.Services;
using Microsoft.AspNetCore.Mvc;

namespace AutoStock.WEB.Controllers
{
    public class AdminController : BaseController
    {
        private readonly AdminWorkshopApiService _adminWorkshopApiService;
        private readonly AdminWorkshopPageService _adminWorkshopPageService;
        private readonly AdminWorkshopInviteEmailService _adminWorkshopInviteEmailService;

        public AdminController(
            AdminWorkshopApiService adminWorkshopApiService,
            AdminWorkshopPageService adminWorkshopPageService,
            AdminWorkshopInviteEmailService adminWorkshopInviteEmailService)
        {
            _adminWorkshopApiService = adminWorkshopApiService;
            _adminWorkshopPageService = adminWorkshopPageService;
            _adminWorkshopInviteEmailService = adminWorkshopInviteEmailService;
        }
        [HttpGet("Admin")]
        [HttpGet("Admin/Dashboard")]
        public IActionResult Dashboard()
        {
            if (!IsAdmin)
                return RedirectToLogin();

            return View();
        }

        [HttpGet("Admin/Index")]
        public IActionResult Index()
        {
            return RedirectToAction(nameof(Dashboard));
        }

        [HttpGet("Admin/Workshops")]
        public async Task<IActionResult> Workshops(AdminWorkshopListQueryViewModel query)
        {
            if (!IsAdmin)
                return RedirectToLogin();

            var pageResult = await _adminWorkshopPageService.GetIndexPageAsync(query);

            if (pageResult.HasErrors)
                ShowErrors(pageResult.ErrorMessages);

            return View(pageResult.ViewModel);
        }

        [HttpGet("Admin/Workshops/Create")]
        public IActionResult CreateWorkshop()
        {
            if (!IsAdmin)
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
            if (!IsAdmin)
                return RedirectToLogin();

            if (!ModelState.IsValid)
                return View(model);

            var result = await _adminWorkshopApiService.CreateAsync(model);

            if (result.IsFailure || result.Data == null)
            {
                if (result.ErrorMessages.Any())
                    ShowErrors(result.ErrorMessages);
                else
                    ShowError(result.ErrorMessage ?? "Servis oluşturulurken hata oluştu.");

                return View(model);
            }

            StoreCreatedUserInviteTempData(result.Data);

            var setupUrl = Url.Action(
    "PasswordSetup",
    "Auth",
    new { token = result.Data.PasswordSetupToken },
    Request.Scheme) ?? string.Empty;

            var inviteMailResult = await _adminWorkshopInviteEmailService.SendCreatedUserInviteAsync(
                result.Data,
                setupUrl);

            if (!inviteMailResult.IsSuccess)
            {
                ShowError(inviteMailResult.ErrorMessage ?? "Davet e-postası gönderilemedi. Bağlantı ekranda gösterilecek.");
            }
            else if (!string.IsNullOrWhiteSpace(result.Data.Email))
            {
                ShowSuccess("Davet e-postası kullanıcıya gönderildi.");
            }

            ShowSuccess("Servis başarıyla oluşturuldu. İlk kullanıcı için davet bağlantısı oluşturuldu.");

            return RedirectToAction(nameof(WorkshopDetails), new { id = result.Data.WorkshopId });
        }

        [HttpGet("Admin/Workshops/Details/{id:int}")]
        public async Task<IActionResult> WorkshopDetails(int id)
        {
            if (!IsAdmin)
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
            if (!IsAdmin)
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
            if (!IsAdmin)
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
            if (!IsAdmin)
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
            if (!IsAdmin)
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
            if (!IsAdmin)
                return RedirectToLogin();

            var pageResult = await _adminWorkshopPageService.GetCreateUserPageAsync(workshopId);

            if (pageResult.HasErrors)
            {
                ShowErrors(pageResult.ErrorMessages);
                return RedirectToAction(nameof(Workshops));
            }

            return View(pageResult.ViewModel);
        }

        [HttpPost("Admin/Workshops/{workshopId:int}/Users/Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateWorkshopUser(int workshopId, CreateAdminWorkshopUserViewModel model)
        {
            if (!IsAdmin)
                return RedirectToLogin();

            model.WorkshopId = workshopId;

            if (!ModelState.IsValid)
            {
                var invalidPageResult = await _adminWorkshopPageService.PrepareCreateUserPageAsync(model);

                if (invalidPageResult.HasErrors)
                    ShowErrors(invalidPageResult.ErrorMessages);

                return View(invalidPageResult.ViewModel);
            }

            var result = await _adminWorkshopPageService.CreateUserAsync(workshopId, model);

            if (result.IsFailure || result.Data == null)
            {
                var failedPageResult = await _adminWorkshopPageService.PrepareCreateUserPageAsync(model);

                ShowError(result.ErrorMessage ?? "Kullanıcı oluşturulurken hata oluştu.");

                return View(failedPageResult.ViewModel);
            }

            StoreCreatedUserInviteTempData(result.Data);

            var setupUrl = Url.Action(
   "PasswordSetup",
   "Auth",
   new { token = result.Data.PasswordSetupToken },
   Request.Scheme) ?? string.Empty;

            var inviteMailResult = await _adminWorkshopInviteEmailService.SendCreatedUserInviteAsync(
                result.Data,
                setupUrl);

            if (!inviteMailResult.IsSuccess)
            {
                ShowError(inviteMailResult.ErrorMessage ?? "Davet e-postası gönderilemedi. Bağlantı ekranda gösterilecek.");
            }
            else if (!string.IsNullOrWhiteSpace(result.Data.Email))
            {
                ShowSuccess("Davet e-postası kullanıcıya gönderildi.");
            }

            ShowSuccess("Kullanıcı başarıyla oluşturuldu. Davet bağlantısı oluşturuldu.");

            return RedirectToAction(nameof(WorkshopDetails), new { id = workshopId });
        }

        [HttpPost("Admin/Workshops/UpdateUserStatus")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateWorkshopUserStatus(int workshopId, int userId, bool isActive)
        {
            if (!IsAdmin)
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

        [HttpGet("Admin/Workshops/{workshopId:int}/Users/{userId:int}")]
        public async Task<IActionResult> WorkshopUserDetails(int workshopId, int userId)
        {
            if (!IsAdmin)
                return RedirectToLogin();

            if (workshopId <= 0 || userId <= 0)
            {
                ShowError("Geçersiz kullanıcı bilgisi.");
                return RedirectToAction(nameof(Workshops));
            }

            var result = await _adminWorkshopApiService.GetUserDetailAsync(workshopId, userId);

            if (result.IsFailure || result.Data == null)
            {
                ShowError(result.ErrorMessage ?? "Kullanıcı detayı alınırken hata oluştu.");
                return RedirectToAction(nameof(WorkshopDetails), new { id = workshopId });
            }

            return View("WorkshopUserDetails", result.Data);
        }

        [HttpPost("Admin/Workshops/{workshopId:int}/Users/{userId:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateWorkshopUser(int workshopId, int userId, AdminWorkshopUserDetailViewModel model)
        {
            if (!IsAdmin)
                return RedirectToLogin();

            if (workshopId <= 0 || userId <= 0 || workshopId != model.WorkshopId || userId != model.UserId)
            {
                ShowError("Geçersiz kullanıcı bilgisi.");
                return RedirectToAction(nameof(Workshops));
            }

            if (!ModelState.IsValid)
            {
                ShowError("Lütfen zorunlu alanları kontrol edin.");
                return View("WorkshopUserDetails", model);
            }

            var result = await _adminWorkshopApiService.UpdateUserAsync(model);

            if (result.IsFailure)
            {
                if (result.ErrorMessages.Any())
                    ShowErrors(result.ErrorMessages);
                else
                    ShowError(result.ErrorMessage ?? "Kullanıcı bilgileri güncellenirken hata oluştu.");

                return View("WorkshopUserDetails", model);
            }

            ShowSuccess("Kullanıcı bilgileri güncellendi.");

            return RedirectToAction(nameof(WorkshopUserDetails), new { workshopId, userId });
        }

        [HttpPost("Admin/Workshops/{workshopId:int}/Users/{userId:int}/PasswordResetLink")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateWorkshopUserPasswordResetLink(int workshopId, int userId)
        {
            if (!IsAdmin)
                return RedirectToLogin();

            if (workshopId <= 0 || userId <= 0)
            {
                ShowError("Geçersiz kullanıcı bilgisi.");
                return RedirectToAction(nameof(Workshops));
            }

            var result = await _adminWorkshopApiService.CreateUserPasswordResetLinkAsync(
                workshopId,
                userId);

            if (result.IsFailure || result.Data == null)
            {
                if (result.ErrorMessages.Any())
                    ShowErrors(result.ErrorMessages);
                else
                    ShowError(result.ErrorMessage ?? "Şifre sıfırlama bağlantısı oluşturulurken hata oluştu.");

                return RedirectToAction(nameof(WorkshopUserDetails), new { workshopId, userId });
            }

            StorePasswordResetLinkTempData(result.Data);

            ShowSuccess("Şifre sıfırlama bağlantısı oluşturuldu.");

            return RedirectToAction(nameof(WorkshopUserDetails), new { workshopId, userId });
        }


        [HttpPost("Admin/Workshops/AddBankAccount")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddWorkshopBankAccount(CreateAdminWorkshopBankAccountViewModel model)
        {
            if (!IsAdmin)
                return RedirectToLogin();

            if (model.WorkshopId <= 0)
            {
                ShowError("Geçersiz servis bilgisi.");
                return RedirectToAction(nameof(Workshops));
            }

            var result = await _adminWorkshopApiService.CreateBankAccountAsync(model);

            return HandleCommandResult(
                result,
                onSuccess: () => RedirectToAction(nameof(WorkshopDetails), new { id = model.WorkshopId }),
                onFailure: () => RedirectToAction(nameof(WorkshopDetails), new { id = model.WorkshopId }),
                defaultErrorMessage: "Banka hesabı eklenirken hata oluştu.",
                successMessage: "Banka hesabı eklendi.");
        }

        [HttpPost("Admin/Workshops/UpdateBankAccount")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateWorkshopBankAccount(UpdateAdminWorkshopBankAccountViewModel model)
        {
            if (!IsAdmin)
                return RedirectToLogin();

            if (model.WorkshopId <= 0 || model.BankAccountId <= 0)
            {
                ShowError("Geçersiz banka hesabı bilgisi.");
                return RedirectToAction(nameof(Workshops));
            }

            var result = await _adminWorkshopApiService.UpdateBankAccountAsync(model);

            return HandleCommandResult(
                result,
                onSuccess: () => RedirectToAction(nameof(WorkshopDetails), new { id = model.WorkshopId }),
                onFailure: () => RedirectToAction(nameof(WorkshopDetails), new { id = model.WorkshopId }),
                defaultErrorMessage: "Banka hesabı güncellenirken hata oluştu.",
                successMessage: "Banka hesabı güncellendi.");
        }

        [HttpPost("Admin/Workshops/DeleteBankAccount")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteWorkshopBankAccount(int workshopId, int bankAccountId)
        {
            if (!IsAdmin)
                return RedirectToLogin();

            if (workshopId <= 0 || bankAccountId <= 0)
            {
                ShowError("Geçersiz banka hesabı bilgisi.");
                return RedirectToAction(nameof(Workshops));
            }

            var result = await _adminWorkshopApiService.DeleteBankAccountAsync(
                workshopId,
                bankAccountId);

            return HandleCommandResult(
                result,
                onSuccess: () => RedirectToAction(nameof(WorkshopDetails), new { id = workshopId }),
                onFailure: () => RedirectToAction(nameof(WorkshopDetails), new { id = workshopId }),
                defaultErrorMessage: "Banka hesabı silinirken hata oluştu.",
                successMessage: "Banka hesabı silindi.");
        }

        [HttpGet("Admin/Workshops/{workshopId:int}/Users/SuggestCredentials")]
        public async Task<IActionResult> SuggestWorkshopUserCredentials(int workshopId, string fullName)
        {
            if (!IsAdmin)
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
                    errorMessage = result.ErrorMessage ?? "Kullanıcı adı önerisi oluşturulamadı."
                });
            }

            return Json(new
            {
                isSuccess = true,
                userName = result.Data.UserName,
                password = result.Data.Password
            });
        }

        


        private void StoreCreatedUserInviteTempData(AdminWorkshopUserCreatedViewModel createdUser)
        {
            TempData["CreatedUserFullName"] = createdUser.FullName;
            TempData["CreatedUserName"] = createdUser.UserName;
            TempData["CreatedUserPhone"] = createdUser.PhoneNumber;
            TempData["CreatedUserPasswordSetupToken"] = createdUser.PasswordSetupToken;
            TempData["CreatedUserPasswordSetupCode"] = createdUser.PasswordSetupCode;
            TempData["CreatedUserPasswordSetupExpiresAt"] =
                createdUser.PasswordSetupExpiresAt.ToString("O");
        }

        private void StorePasswordResetLinkTempData(AdminWorkshopUserPasswordResetLinkViewModel resetLink)
        {
            TempData["ResetUserFullName"] = resetLink.FullName;
            TempData["ResetUserName"] = resetLink.UserName;
            TempData["ResetUserPhone"] = resetLink.PhoneNumber;
            TempData["ResetPasswordToken"] = resetLink.PasswordResetToken;
            TempData["ResetPasswordCode"] = resetLink.PasswordResetCode;
            TempData["ResetPasswordExpiresAt"] = resetLink.PasswordResetExpiresAt.ToString("O");
        }
    }
}
