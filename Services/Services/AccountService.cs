using AutoStock.Repositories;
using AutoStock.Repositories.Entities;
using AutoStock.Repositories.Enums;
using AutoStock.Services.Constants;
using AutoStock.Services.Dtos.Account;
using AutoStock.Services.Dtos.AuditLogs;
using AutoStock.Services.Dtos.Common;
using AutoStock.Services.Dtos.Emails;
using AutoStock.Services.Dtos.Notifications;
using AutoStock.Services.Dtos.SecurityTokens;
using AutoStock.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Net;
using System.Net.Mail;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;

namespace AutoStock.Services.Services
{
    public class AccountService : IAccountService
    {
        private const string ForgotPasswordGenericMessage =
            "Hesap bilgileri eşleşiyorsa şifre yenileme işlemi başlatıldı. Kayıtlı e-posta adresinizi kontrol edin. Yardıma ihtiyaç duyarsanız Sente360 desteğiyle iletişime geçebilirsiniz.";

        private static readonly TimeSpan ForgotPasswordTokenLifetime = TimeSpan.FromMinutes(60);
        private static readonly TimeSpan ForgotPasswordCooldown = TimeSpan.FromMinutes(15);

        private readonly UserManager<AppUser> _userManager;
        private readonly AppDbContext _context;
        private readonly IUserSecurityTokenService _userSecurityTokenService;
        private readonly IEmailSender _emailSender;
        private readonly INotificationService _notificationService;
        private readonly IAuditLogService _auditLogService;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly ILogger<AccountService> _logger;

        public AccountService(
            UserManager<AppUser> userManager,
            AppDbContext context,
            IUserSecurityTokenService userSecurityTokenService,
            IEmailSender emailSender,
            INotificationService notificationService,
            IAuditLogService auditLogService,
            IDateTimeProvider dateTimeProvider,
            ILogger<AccountService> logger)
        {
            _userManager = userManager;
            _context = context;
            _userSecurityTokenService = userSecurityTokenService;
            _emailSender = emailSender;
            _notificationService = notificationService;
            _auditLogService = auditLogService;
            _dateTimeProvider = dateTimeProvider;
            _logger = logger;
        }

        public async Task<ServiceResult<AccountOverviewDto>> GetOverviewAsync(int userId)
        {
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == userId);

            if (user is null || !user.IsActive)
                return ServiceResult<AccountOverviewDto>.Fail("Kullanıcı bulunamadı.", HttpStatusCode.NotFound);

            var roles = await _userManager.GetRolesAsync(user);
            var role = GetPrimaryRole(roles);

            var workshopName = string.Empty;

            if (role != AppRoles.Admin)
            {
                workshopName = await _context.WorkshopUsers
                    .AsNoTracking()
                    .Where(x => x.UserId == userId)
                    .Select(x => x.Workshop.Name)
                    .FirstOrDefaultAsync() ?? string.Empty;
            }

            return ServiceResult<AccountOverviewDto>.Success(new AccountOverviewDto
            {
                FullName = user.FullName,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email,
                EmailConfirmed = user.EmailConfirmed,
                PhoneNumber = user.PhoneNumber,
                Role = role,
                WorkshopName = workshopName
            });
        }

        public async Task<ServiceResult<bool>> UpdateEmailAsync(int userId, UpdateAccountEmailRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.CurrentPassword))
                return ServiceResult<bool>.Fail("E-posta değişikliği için mevcut şifrenizi girin.");

            var user = await _userManager.FindByIdAsync(userId.ToString());

            if (user is null || !user.IsActive)
                return ServiceResult<bool>.Fail("Kullanıcı bulunamadı.", HttpStatusCode.NotFound);

            if (!await _userManager.CheckPasswordAsync(user, request.CurrentPassword))
                return ServiceResult<bool>.Fail("Mevcut şifre hatalı.");

            var email = NormalizeNullable(request.Email, 150);

            if (!string.IsNullOrWhiteSpace(email))
            {
                try
                {
                    _ = new MailAddress(email);
                }
                catch
                {
                    return ServiceResult<bool>.Fail("Geçerli bir e-posta adresi girin.");
                }

                var existingUser = await _userManager.FindByEmailAsync(email);

                if (existingUser is not null && existingUser.Id != user.Id)
                    return ServiceResult<bool>.Fail("Bu e-posta adresi başka bir kullanıcıda kayıtlı.");
            }

            var oldEmail = user.Email;
            user.Email = email;
            user.NormalizedEmail = string.IsNullOrWhiteSpace(email)
                ? null
                : _userManager.NormalizeEmail(email);
            user.EmailConfirmed = false;

            var updateResult = await _userManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
                return ServiceResult<bool>.Fail(MapIdentityErrors(updateResult));

            await WriteUserAuditAsync(user, "Kullanıcı e-posta adresini güncelledi", new
            {
                OldEmail = oldEmail,
                NewEmail = email
            });

            return ServiceResult<bool>.Success(true);
        }

        public async Task<ServiceResult<bool>> UpdatePhoneAsync(int userId, UpdateAccountPhoneRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.CurrentPassword))
                return ServiceResult<bool>.Fail("Telefon değişikliği için mevcut şifrenizi girin.");

            var user = await _userManager.FindByIdAsync(userId.ToString());

            if (user is null || !user.IsActive)
                return ServiceResult<bool>.Fail("Kullanıcı bulunamadı.", HttpStatusCode.NotFound);

            if (!await _userManager.CheckPasswordAsync(user, request.CurrentPassword))
                return ServiceResult<bool>.Fail("Mevcut şifre hatalı.");

            var normalizedPhone = NormalizeTurkishMobilePhone(request.PhoneNumber);

            if (!string.IsNullOrWhiteSpace(request.PhoneNumber) && normalizedPhone is null)
                return ServiceResult<bool>.Fail("Geçerli bir cep telefonu numarası girin.");

            var oldPhone = user.PhoneNumber;
            var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, normalizedPhone);

            if (!setPhoneResult.Succeeded)
                return ServiceResult<bool>.Fail(MapIdentityErrors(setPhoneResult));

            user.PhoneNumberConfirmed = false;

            var updateResult = await _userManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
                return ServiceResult<bool>.Fail(MapIdentityErrors(updateResult));

            await WriteUserAuditAsync(user, "Kullanıcı telefon numarasını güncelledi", new
            {
                OldPhone = MaskPhone(oldPhone),
                NewPhone = MaskPhone(normalizedPhone)
            });

            return ServiceResult<bool>.Success(true);
        }

        public async Task<ServiceResult<bool>> ChangePasswordAsync(int userId, ChangePasswordRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.CurrentPassword))
                return ServiceResult<bool>.Fail("Mevcut şifre zorunludur.");

            if (string.IsNullOrWhiteSpace(request.NewPassword))
                return ServiceResult<bool>.Fail("Yeni şifre zorunludur.");

            if (request.NewPassword != request.ConfirmNewPassword)
                return ServiceResult<bool>.Fail("Şifreler eşleşmiyor.");

            if (request.NewPassword == request.CurrentPassword)
                return ServiceResult<bool>.Fail("Yeni şifre mevcut şifreyle aynı olamaz.");

            var user = await _userManager.FindByIdAsync(userId.ToString());

            if (user is null || !user.IsActive)
                return ServiceResult<bool>.Fail("Kullanıcı bulunamadı.", HttpStatusCode.NotFound);

            var result = await _userManager.ChangePasswordAsync(
                user,
                request.CurrentPassword,
                request.NewPassword);

            if (!result.Succeeded)
                return ServiceResult<bool>.Fail(MapIdentityErrors(result));

            user.PasswordChangedAt = _dateTimeProvider.Now;
            await _userManager.UpdateAsync(user);

            await WriteUserAuditAsync(user, "Kullanıcı kendi şifresini değiştirdi", new
            {
                PasswordChangedAt = user.PasswordChangedAt
            });

            return ServiceResult<bool>.Success(true);
        }

        public async Task<ServiceResult<bool>> SendEmailConfirmationAsync(
            int userId,
            RequestEmailConfirmationDto request)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());

            if (user is null || !user.IsActive)
                return ServiceResult<bool>.Fail("Kullanıcı bulunamadı.", HttpStatusCode.NotFound);

            if (string.IsNullOrWhiteSpace(user.Email))
                return ServiceResult<bool>.Fail("Önce hesabınıza geçerli bir e-posta adresi ekleyin.");

            if (user.EmailConfirmed)
                return ServiceResult<bool>.Success(true);

            if (string.IsNullOrWhiteSpace(request.ConfirmationUrlBase))
                return ServiceResult<bool>.Fail("E-posta doğrulama bağlantısı hazırlanamadı.");

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var confirmationUrl = BuildEmailConfirmationUrl(
                request.ConfirmationUrlBase,
                user.Id,
                token);

            var emailResult = await _emailSender.SendAsync(new EmailMessageDto
            {
                ToEmail = user.Email,
                ToName = user.FullName,
                Subject = "Sente360 E-posta Doğrulama",
                HtmlBody = BuildEmailConfirmationBody(user.FullName, confirmationUrl)
            });

            if (emailResult.IsFailure)
            {
                _logger.LogWarning(
                    "Email confirmation message could not be sent. UserId: {UserId}, Error: {Error}",
                    user.Id,
                    emailResult.ErrorMessage);

                return ServiceResult<bool>.Fail(
                    "Doğrulama e-postası gönderilemedi. Lütfen kısa süre sonra tekrar deneyin.");
            }

            return ServiceResult<bool>.Success(true);
        }

        public async Task<ServiceResult<bool>> ConfirmEmailAsync(ConfirmEmailDto request)
        {
            if (request.UserId <= 0 || string.IsNullOrWhiteSpace(request.Token))
                return ServiceResult<bool>.Fail("E-posta doğrulama bağlantısı geçersiz.");

            var user = await _userManager.FindByIdAsync(request.UserId.ToString());

            if (user is null || !user.IsActive)
                return ServiceResult<bool>.Fail("E-posta doğrulama bağlantısı geçersiz.");

            if (user.EmailConfirmed)
                return ServiceResult<bool>.Success(true);

            var result = await _userManager.ConfirmEmailAsync(user, request.Token);

            if (!result.Succeeded)
                return ServiceResult<bool>.Fail(
                    "E-posta doğrulama bağlantısı geçersiz veya süresi dolmuş.");

            user.LastPasswordResetAt = null;
            await _userManager.UpdateAsync(user);

            await WriteUserAuditAsync(user, "Kullanıcı e-posta adresini doğruladı", new
            {
                user.Email,
                ConfirmedAt = _dateTimeProvider.Now
            });

            return ServiceResult<bool>.Success(true);
        }

        public async Task<ServiceResult<WorkshopProfileManagementDto>> GetWorkshopProfileAsync(int userId, int workshopId)
        {
            if (!await IsWorkshopOwnerAsync(userId, workshopId))
                return ServiceResult<WorkshopProfileManagementDto>.Fail(
                    "Bu alan için servis sahibi yetkisi gerekir.",
                    HttpStatusCode.Forbidden);

            var workshop = await _context.Workshops
                .AsNoTracking()
                .Include(x => x.Profile)
                .FirstOrDefaultAsync(x => x.Id == workshopId);

            if (workshop is null)
                return ServiceResult<WorkshopProfileManagementDto>.Fail("Servis bulunamadı.", HttpStatusCode.NotFound);

            var bankAccounts = await _context.WorkshopBankAccounts
                .AsNoTracking()
                .Where(x =>
                    x.WorkshopId == workshopId &&
                    x.IsActive)
                .OrderByDescending(x => x.IsDefault)
                .ThenBy(x => x.SortOrder)
                .ThenBy(x => x.BankName)
                .Select(x => new AccountWorkshopBankAccountDto
                {
                    Id = x.Id,
                    BankName = x.BankName,
                    AccountHolder = x.AccountHolder,
                    Iban = x.Iban,
                    CurrencyCode = x.CurrencyCode,
                    BranchName = x.BranchName,
                    AccountNumber = x.AccountNumber,
                    Description = x.Description,
                    IsDefault = x.IsDefault,
                    ShowOnInvoices = x.ShowOnInvoices,
                    ShowOnServiceForms = x.ShowOnServiceForms,
                    SortOrder = x.SortOrder
                })
                .ToListAsync();

            return ServiceResult<WorkshopProfileManagementDto>.Success(new WorkshopProfileManagementDto
            {
                WorkshopName = workshop.Name,
                DisplayName = workshop.Profile?.DisplayName,
                LegalTitle = workshop.Profile?.LegalTitle,
                TaxOffice = workshop.Profile?.TaxOffice,
                TaxNumber = workshop.Profile?.TaxNumber,
                Email = workshop.Profile?.Email,
                PhoneNumber = workshop.Profile?.PhoneNumber,
                Website = workshop.Profile?.Website,
                AddressLine = workshop.Profile?.AddressLine,
                City = workshop.Profile?.City,
                District = workshop.Profile?.District,
                PostalCode = workshop.Profile?.PostalCode,
                Country = workshop.Profile?.Country,
                BankAccounts = bankAccounts
            });
        }

        public async Task<ServiceResult<bool>> UpdateWorkshopProfileAsync(
            int userId,
            int workshopId,
            UpdateWorkshopProfileManagementRequestDto request)
        {
            if (!await IsWorkshopOwnerAsync(userId, workshopId))
                return ServiceResult<bool>.Fail(
                    "Bu alan için servis sahibi yetkisi gerekir.",
                    HttpStatusCode.Forbidden);

            var workshop = await _context.Workshops
                .Include(x => x.Profile)
                .FirstOrDefaultAsync(x => x.Id == workshopId);

            if (workshop is null)
                return ServiceResult<bool>.Fail("Servis bulunamadı.", HttpStatusCode.NotFound);

            workshop.Profile ??= new WorkshopProfile
            {
                WorkshopId = workshopId,
                CreatedAt = _dateTimeProvider.Now,
                Country = "Türkiye"
            };

            workshop.Profile.DisplayName = NormalizeNullable(request.DisplayName, 200);
            workshop.Profile.LegalTitle = NormalizeNullable(request.LegalTitle, 300);
            workshop.Profile.TaxOffice = NormalizeNullable(request.TaxOffice, 100);
            workshop.Profile.TaxNumber = NormalizeNullable(request.TaxNumber, 20);
            workshop.Profile.Email = NormalizeNullable(request.Email, 150);
            workshop.Profile.PhoneNumber = NormalizeNullable(request.PhoneNumber, 30);
            workshop.Profile.Website = NormalizeNullable(request.Website, 150);
            workshop.Profile.AddressLine = NormalizeNullable(request.AddressLine, 500);
            workshop.Profile.City = NormalizeNullable(request.City, 100);
            workshop.Profile.District = NormalizeNullable(request.District, 100);
            workshop.Profile.PostalCode = NormalizeNullable(request.PostalCode, 20);
            workshop.Profile.Country = NormalizeNullable(request.Country, 100) ?? "Türkiye";
            workshop.Profile.UpdatedAt = _dateTimeProvider.Now;

            await _context.SaveChangesAsync();

            await _auditLogService.WriteAsync(new AuditLogCreateDto
            {
                WorkshopId = workshopId,
                UserId = userId,
                UserRole = AppRoles.Owner,
                ActionType = AuditActionType.Update,
                EntityType = AuditEntityType.Workshop,
                EntityId = workshopId,
                Description = "Servis bilgileri kullanıcı hesabı üzerinden güncellendi",
                NewValues = new
                {
                    workshop.Profile.DisplayName,
                    workshop.Profile.LegalTitle,
                    workshop.Profile.TaxNumber,
                    workshop.Profile.Email,
                    workshop.Profile.PhoneNumber
                }
            });

            return ServiceResult<bool>.Success(true);
        }

        public async Task<ServiceResult<int>> CreateWorkshopBankAccountAsync(
            int userId,
            int workshopId,
            CreateAccountWorkshopBankAccountRequestDto request)
        {
            if (!await IsWorkshopOwnerAsync(userId, workshopId))
                return ServiceResult<int>.Fail(
                    "Bu alan için servis sahibi yetkisi gerekir.",
                    HttpStatusCode.Forbidden);

            var workshopExists = await _context.Workshops
                .AsNoTracking()
                .AnyAsync(x => x.Id == workshopId);

            if (!workshopExists)
                return ServiceResult<int>.Fail("Servis bulunamadı.", HttpStatusCode.NotFound);

            var validationResult = ValidateBankAccountRequest(
                request.BankName,
                request.AccountHolder,
                request.Iban,
                request.CurrencyCode);

            if (validationResult.IsFailure)
                return ServiceResult<int>.Fail(validationResult.ErrorMessages);

            var normalizedIban = NormalizeIban(request.Iban);
            var currencyCode = NormalizeCurrencyCode(request.CurrencyCode);

            var duplicateExists = await _context.WorkshopBankAccounts
                .AnyAsync(x =>
                    x.WorkshopId == workshopId &&
                    x.IsActive &&
                    x.Iban == normalizedIban);

            if (duplicateExists)
                return ServiceResult<int>.Fail("Bu IBAN zaten eklenmiş.");

            await using var transaction = await _context.Database.BeginTransactionAsync();

            if (request.IsDefault)
            {
                await ClearDefaultBankAccountsAsync(workshopId);
            }

            var account = new WorkshopBankAccount
            {
                WorkshopId = workshopId,
                BankName = request.BankName.Trim(),
                AccountHolder = request.AccountHolder.Trim(),
                Iban = normalizedIban,
                CurrencyCode = currencyCode,
                BranchName = NormalizeNullable(request.BranchName, 100),
                AccountNumber = NormalizeNullable(request.AccountNumber, 50),
                Description = NormalizeNullable(request.Description, 250),
                IsDefault = request.IsDefault,
                ShowOnInvoices = request.ShowOnInvoices,
                ShowOnServiceForms = request.ShowOnServiceForms,
                IsActive = true,
                SortOrder = request.SortOrder,
                CreatedAt = _dateTimeProvider.Now
            };

            _context.WorkshopBankAccounts.Add(account);
            await _context.SaveChangesAsync();

            await WriteBankAccountAuditAsync(
                userId,
                workshopId,
                AuditActionType.Create,
                $"Banka hesabı eklendi: {account.BankName} / {account.Iban}",
                null,
                GetBankAccountAuditValues(account));

            await transaction.CommitAsync();

            return ServiceResult<int>.Success(account.Id);
        }

        public async Task<ServiceResult<bool>> UpdateWorkshopBankAccountAsync(
            int userId,
            int workshopId,
            int bankAccountId,
            UpdateAccountWorkshopBankAccountRequestDto request)
        {
            if (!await IsWorkshopOwnerAsync(userId, workshopId))
                return ServiceResult<bool>.Fail(
                    "Bu alan için servis sahibi yetkisi gerekir.",
                    HttpStatusCode.Forbidden);

            var account = await _context.WorkshopBankAccounts
                .FirstOrDefaultAsync(x =>
                    x.Id == bankAccountId &&
                    x.WorkshopId == workshopId &&
                    x.IsActive);

            if (account is null)
                return ServiceResult<bool>.Fail("Banka hesabı bulunamadı.", HttpStatusCode.NotFound);

            var validationResult = ValidateBankAccountRequest(
                request.BankName,
                request.AccountHolder,
                request.Iban,
                request.CurrencyCode);

            if (validationResult.IsFailure)
                return ServiceResult<bool>.Fail(validationResult.ErrorMessages);

            var normalizedIban = NormalizeIban(request.Iban);
            var currencyCode = NormalizeCurrencyCode(request.CurrencyCode);

            var duplicateExists = await _context.WorkshopBankAccounts
                .AnyAsync(x =>
                    x.WorkshopId == workshopId &&
                    x.Id != bankAccountId &&
                    x.IsActive &&
                    x.Iban == normalizedIban);

            if (duplicateExists)
                return ServiceResult<bool>.Fail("Bu IBAN başka bir banka hesabında kullanılıyor.");

            await using var transaction = await _context.Database.BeginTransactionAsync();

            var oldValues = GetBankAccountAuditValues(account);

            if (request.IsDefault)
            {
                await ClearDefaultBankAccountsAsync(workshopId, bankAccountId);
            }

            account.BankName = request.BankName.Trim();
            account.AccountHolder = request.AccountHolder.Trim();
            account.Iban = normalizedIban;
            account.CurrencyCode = currencyCode;
            account.BranchName = NormalizeNullable(request.BranchName, 100);
            account.AccountNumber = NormalizeNullable(request.AccountNumber, 50);
            account.Description = NormalizeNullable(request.Description, 250);
            account.IsDefault = request.IsDefault;
            account.ShowOnInvoices = request.ShowOnInvoices;
            account.ShowOnServiceForms = request.ShowOnServiceForms;
            account.IsActive = request.IsActive;
            account.SortOrder = request.SortOrder;
            account.UpdatedAt = _dateTimeProvider.Now;

            await _context.SaveChangesAsync();

            await WriteBankAccountAuditAsync(
                userId,
                workshopId,
                AuditActionType.Update,
                $"Banka hesabı güncellendi: {account.BankName} / {account.Iban}",
                oldValues,
                GetBankAccountAuditValues(account));

            await transaction.CommitAsync();

            return ServiceResult<bool>.Success(true);
        }

        public async Task<ServiceResult<bool>> DeleteWorkshopBankAccountAsync(
            int userId,
            int workshopId,
            int bankAccountId)
        {
            if (!await IsWorkshopOwnerAsync(userId, workshopId))
                return ServiceResult<bool>.Fail(
                    "Bu alan için servis sahibi yetkisi gerekir.",
                    HttpStatusCode.Forbidden);

            var account = await _context.WorkshopBankAccounts
                .FirstOrDefaultAsync(x =>
                    x.Id == bankAccountId &&
                    x.WorkshopId == workshopId &&
                    x.IsActive);

            if (account is null)
                return ServiceResult<bool>.Fail("Banka hesabı bulunamadı.", HttpStatusCode.NotFound);

            var oldValues = GetBankAccountAuditValues(account);

            account.IsActive = false;
            account.IsDefault = false;
            account.UpdatedAt = _dateTimeProvider.Now;

            await _context.SaveChangesAsync();

            await WriteBankAccountAuditAsync(
                userId,
                workshopId,
                AuditActionType.Remove,
                $"Banka hesabı pasife alındı: {account.BankName} / {account.Iban}",
                oldValues,
                GetBankAccountAuditValues(account));

            return ServiceResult<bool>.Success(true);
        }

        public async Task<ServiceResult<MembershipInfoDto>> GetMembershipAsync(int userId, int workshopId)
        {
            if (!await IsWorkshopOwnerAsync(userId, workshopId))
                return ServiceResult<MembershipInfoDto>.Fail(
                    "Bu alan için servis sahibi yetkisi gerekir.",
                    HttpStatusCode.Forbidden);

            var workshop = await _context.Workshops
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == workshopId);

            if (workshop is null)
                return ServiceResult<MembershipInfoDto>.Fail("Servis bulunamadı.", HttpStatusCode.NotFound);

            var activeUserCount = await _context.WorkshopUsers
                .AsNoTracking()
                .Where(x => x.WorkshopId == workshopId && x.User.IsActive)
                .Select(x => x.UserId)
                .Distinct()
                .CountAsync();

            int? remainingDays = null;

            if (workshop.SubscriptionEndDate.HasValue)
            {
                var remaining = workshop.SubscriptionEndDate.Value.Date - _dateTimeProvider.Now.Date;
                remainingDays = Math.Max(0, remaining.Days);
            }

            return ServiceResult<MembershipInfoDto>.Success(new MembershipInfoDto
            {
                WorkshopName = workshop.Name,
                Status = workshop.SubscriptionStatus,
                StatusText = GetSubscriptionStatusText(workshop.SubscriptionStatus),
                StatusDescription = GetSubscriptionStatusDescription(workshop.SubscriptionStatus),
                SummaryText = BuildMembershipSummary(workshop.SubscriptionStatus, workshop.SubscriptionEndDate, remainingDays),
                SubscriptionStartDate = workshop.SubscriptionStartDate,
                SubscriptionStartDateText = FormatLongDate(workshop.SubscriptionStartDate),
                SubscriptionEndDate = workshop.SubscriptionEndDate,
                ValidityText = workshop.SubscriptionEndDate.HasValue
                    ? $"{FormatLongDate(workshop.SubscriptionEndDate.Value)}'ya kadar"
                    : "Bitiş tarihi henüz tanımlanmamış",
                RemainingText = workshop.SubscriptionEndDate.HasValue
                    ? BuildRemainingText(remainingDays)
                    : null,
                RemainingDays = remainingDays,
                IsTrial = workshop.SubscriptionStatus == WorkshopSubscriptionStatus.Trial,
                ActiveUserCount = activeUserCount,
                ActiveUserCountText = activeUserCount == 1
                    ? "1 kullanıcı"
                    : $"{activeUserCount} kullanıcı"
            });
        }

        public async Task<ServiceResult<bool>> StartForgotPasswordAsync(ForgotPasswordRequestDto request)
        {
            var userName = request.UserName?.Trim();

            if (string.IsNullOrWhiteSpace(userName))
                return ServiceResult<bool>.Success(true);

            var user = userName.Contains("@", StringComparison.Ordinal)
                ? await _userManager.FindByEmailAsync(userName)
                : await _userManager.FindByNameAsync(userName);

            if (user is null || !user.IsActive)
                return ServiceResult<bool>.Success(true);

            var now = _dateTimeProvider.Now;

            if (user.LastPasswordResetAt.HasValue &&
                user.LastPasswordResetAt.Value.Add(ForgotPasswordCooldown) > now)
            {
                return ServiceResult<bool>.Success(true);
            }

            var workshopUser = await _context.WorkshopUsers
                .AsNoTracking()
                .Include(x => x.Workshop)
                .FirstOrDefaultAsync(x => x.UserId == user.Id);

            if (string.IsNullOrWhiteSpace(user.Email) || !user.EmailConfirmed)
            {
                user.LastPasswordResetAt = now;
                await _userManager.UpdateAsync(user);

                await NotifyAdminsForPasswordResetEmailProblemAsync(
                    user,
                    workshopUser,
                    string.IsNullOrWhiteSpace(user.Email)
                        ? "missing"
                        : "unconfirmed");
                return ServiceResult<bool>.Success(true);
            }

            var tokenResult = await _userSecurityTokenService.CreateAsync(new CreateUserSecurityTokenRequestDto
            {
                UserId = user.Id,
                Purpose = UserSecurityTokenPurpose.PasswordReset,
                DeliveryChannel = UserSecurityTokenDeliveryChannel.Email,
                ValidFor = ForgotPasswordTokenLifetime
            });

            if (tokenResult.IsFailure || tokenResult.Data is null)
            {
                _logger.LogWarning(
                    "Forgot password token could not be created for UserId: {UserId}. Error: {Error}",
                    user.Id,
                    tokenResult.ErrorMessage);

                return ServiceResult<bool>.Success(true);
            }

            user.LastPasswordResetAt = now;
            await _userManager.UpdateAsync(user);

            var resetUrl = BuildResetUrl(request.ResetUrlBase, tokenResult.Data.Token);

            var emailResult = await _emailSender.SendAsync(new EmailMessageDto
            {
                ToEmail = user.Email,
                ToName = user.FullName,
                Subject = "Sente360 Şifre Yenileme",
                HtmlBody = BuildForgotPasswordEmailBody(user.FullName, resetUrl, tokenResult.Data.ExpiresAt)
            });

            if (emailResult.IsFailure)
            {
                _logger.LogWarning(
                    "Forgot password email could not be sent. UserId: {UserId}, Error: {Error}",
                    user.Id,
                    emailResult.ErrorMessage);

                await NotifyAdminsForPasswordResetEmailProblemAsync(
                    user,
                    workshopUser,
                    "delivery");
            }

            return ServiceResult<bool>.Success(true);
        }

        private async Task<bool> IsWorkshopOwnerAsync(int userId, int workshopId)
        {
            return await _context.WorkshopUsers
                .AsNoTracking()
                .AnyAsync(x =>
                    x.UserId == userId &&
                    x.WorkshopId == workshopId &&
                    x.Role == AppRoles.Owner &&
                    x.User.IsActive);
        }

        private async Task ClearDefaultBankAccountsAsync(
            int workshopId,
            int? exceptBankAccountId = null)
        {
            var currentDefaults = await _context.WorkshopBankAccounts
                .Where(x =>
                    x.WorkshopId == workshopId &&
                    x.IsActive &&
                    x.IsDefault &&
                    (!exceptBankAccountId.HasValue || x.Id != exceptBankAccountId.Value))
                .ToListAsync();

            foreach (var currentDefault in currentDefaults)
            {
                currentDefault.IsDefault = false;
                currentDefault.UpdatedAt = _dateTimeProvider.Now;
            }
        }

        private async Task WriteBankAccountAuditAsync(
            int userId,
            int workshopId,
            AuditActionType actionType,
            string description,
            object? oldValues,
            object newValues)
        {
            await _auditLogService.WriteAsync(new AuditLogCreateDto
            {
                WorkshopId = workshopId,
                UserId = userId,
                UserRole = AppRoles.Owner,
                ActionType = actionType,
                EntityType = AuditEntityType.Workshop,
                EntityId = workshopId,
                Description = description,
                OldValues = oldValues,
                NewValues = newValues
            });
        }

        private async Task NotifyAdminsForPasswordResetEmailProblemAsync(
            AppUser user,
            WorkshopUser? workshopUser,
            string reason)
        {
            var workshopName = workshopUser?.Workshop?.Name ?? "Servis";
            var reasonText = reason switch
            {
                "unconfirmed" => "e-posta adresi doğrulanmadığı",
                "delivery" => "şifre yenileme e-postası gönderilemediği",
                _ => "kayıtlı e-posta adresi olmadığı"
            };

            var notificationResult = await _notificationService.CreateForAdminsAsync(new CreateNotificationDto
            {
                WorkshopId = workshopUser?.WorkshopId,
                Type = NotificationType.System,
                Title = "Şifre yenileme yardımı gerekli",
                Message = $"{workshopName} servisindeki {user.FullName} kullanıcısının {reasonText} için şifre yenileme bağlantısı gönderilemedi.",
                RelatedEntityType = NotificationRelatedEntityType.Workshop,
                RelatedEntityId = workshopUser?.WorkshopId,
                ActionUrl = workshopUser is null
                    ? "/Admin"
                    : $"/Admin/Workshops/{workshopUser.WorkshopId}/Users/{user.Id}"
            });

            if (notificationResult.IsFailure)
            {
                _logger.LogWarning(
                    "Admin notification for forgot password email problem failed. UserId: {UserId}, Reason: {Reason}, Error: {Error}",
                    user.Id,
                    reason,
                    notificationResult.ErrorMessage);
            }
        }

        private async Task WriteUserAuditAsync(AppUser user, string description, object values)
        {
            var workshopUser = await _context.WorkshopUsers
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserId == user.Id);

            await _auditLogService.WriteAsync(new AuditLogCreateDto
            {
                WorkshopId = workshopUser?.WorkshopId,
                UserId = user.Id,
                UserFullName = user.FullName,
                UserRole = workshopUser?.Role,
                ActionType = AuditActionType.Update,
                EntityType = AuditEntityType.Auth,
                EntityId = user.Id,
                Description = description,
                NewValues = values
            });
        }

        private static string BuildResetUrl(string resetUrlBase, string token)
        {
            var baseUrl = string.IsNullOrWhiteSpace(resetUrlBase)
                ? "/Auth/PasswordReset"
                : resetUrlBase.Trim();

            var separator = baseUrl.Contains('?', StringComparison.Ordinal) ? "&" : "?";
            return $"{baseUrl}{separator}token={Uri.EscapeDataString(token)}";
        }

        private static string BuildEmailConfirmationUrl(
            string confirmationUrlBase,
            int userId,
            string token)
        {
            var baseUrl = confirmationUrlBase.Trim();
            var separator = baseUrl.Contains('?', StringComparison.Ordinal) ? "&" : "?";

            return $"{baseUrl}{separator}userId={userId}&token={Uri.EscapeDataString(token)}";
        }

        private static string BuildEmailConfirmationBody(string fullName, string confirmationUrl)
        {
            var safeName = HtmlEncoder.Default.Encode(fullName);
            var safeUrl = HtmlEncoder.Default.Encode(confirmationUrl);

            return $@"
<p>Merhaba {safeName},</p>
<p>Sente360 hesabınızdaki e-posta adresini doğrulamak için aşağıdaki bağlantıyı kullanın.</p>
<p><a href=""{safeUrl}"">E-posta adresimi doğrula</a></p>
<p>Bu işlemi siz başlatmadıysanız bu e-postayı dikkate almayabilirsiniz.</p>";
        }

        private static string BuildForgotPasswordEmailBody(string fullName, string resetUrl, DateTime expiresAt)
        {
            var safeName = HtmlEncoder.Default.Encode(fullName);
            var safeUrl = HtmlEncoder.Default.Encode(resetUrl);
            var safeExpiresAt = HtmlEncoder.Default.Encode(expiresAt.ToString("dd.MM.yyyy HH:mm"));

            return $@"
<p>Merhaba {safeName},</p>
<p>Sente360 hesabınız için şifre yenileme talebi alındı.</p>
<p><a href=""{safeUrl}"">Şifremi yenile</a></p>
<p>Bu bağlantı tek kullanımlıktır ve {safeExpiresAt} tarihine kadar geçerlidir.</p>
<p>Bu işlemi siz başlatmadıysanız bu e-postayı dikkate almayabilirsiniz.</p>";
        }

        private static string GetPrimaryRole(IList<string> roles)
        {
            if (roles.Contains(AppRoles.Admin))
                return AppRoles.Admin;

            if (roles.Contains(AppRoles.Owner))
                return AppRoles.Owner;

            if (roles.Contains(AppRoles.Staff))
                return AppRoles.Staff;

            return AppRoles.Staff;
        }

        private static string GetSubscriptionStatusText(WorkshopSubscriptionStatus status)
        {
            return status switch
            {
                WorkshopSubscriptionStatus.Trial => "Deneme",
                WorkshopSubscriptionStatus.Active => "Aktif",
                WorkshopSubscriptionStatus.Suspended => "Askıya Alındı",
                WorkshopSubscriptionStatus.Expired => "Süresi Doldu",
                WorkshopSubscriptionStatus.Cancelled => "İptal Edildi",
                _ => status.ToString()
            };
        }

        private static string GetSubscriptionStatusDescription(WorkshopSubscriptionStatus status)
        {
            return status switch
            {
                WorkshopSubscriptionStatus.Trial => "Sente360 deneme kullanımınız devam ediyor.",
                WorkshopSubscriptionStatus.Active => "Sente360 hesabınız kullanıma açık.",
                WorkshopSubscriptionStatus.Suspended => "Sente360 hesabınız geçici olarak kullanıma kapatılmış.",
                WorkshopSubscriptionStatus.Expired => "Üyelik süreniz sona ermiş.",
                WorkshopSubscriptionStatus.Cancelled => "Üyeliğiniz iptal edilmiş.",
                _ => "Üyelik durumunuz görüntüleniyor."
            };
        }

        private static string BuildMembershipSummary(
            WorkshopSubscriptionStatus status,
            DateTime? endDate,
            int? remainingDays)
        {
            var statusText = GetSubscriptionStatusText(status);

            if (status == WorkshopSubscriptionStatus.Expired || status == WorkshopSubscriptionStatus.Cancelled)
                return "Süresi sona erdi";

            if (status == WorkshopSubscriptionStatus.Trial && remainingDays.HasValue)
                return $"{statusText} · {BuildRemainingText(remainingDays)}";

            if (endDate.HasValue)
                return $"{statusText} · {FormatLongDate(endDate.Value)}'ya kadar";

            return $"{statusText} · Bitiş tarihi tanımlanmamış";
        }

        private static string BuildRemainingText(int? remainingDays)
        {
            if (!remainingDays.HasValue)
                return string.Empty;

            if (remainingDays.Value <= 0)
                return "Süre sona erdi";

            if (remainingDays.Value == 1)
                return "1 gün kaldı";

            return $"{remainingDays.Value} gün kaldı";
        }

        private static string FormatLongDate(DateTime value)
        {
            return value.ToString("d MMMM yyyy", new CultureInfo("tr-TR"));
        }

        private static string? NormalizeTurkishMobilePhone(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var digits = new string(value.Where(char.IsDigit).ToArray());

            if (digits.StartsWith("90", StringComparison.Ordinal) && digits.Length == 12)
                digits = digits[2..];

            if (digits.StartsWith("5", StringComparison.Ordinal) && digits.Length == 10)
                digits = $"0{digits}";

            if (digits.Length != 11 || !digits.StartsWith("05", StringComparison.Ordinal))
                return null;

            return $"{digits[..4]} {digits.Substring(4, 3)} {digits.Substring(7, 2)} {digits.Substring(9, 2)}";
        }

        private static string? MaskPhone(string? value)
        {
            var digits = new string((value ?? string.Empty).Where(char.IsDigit).ToArray());

            if (digits.Length < 4)
                return string.IsNullOrWhiteSpace(value) ? null : "***";

            return $"*** *** {digits[^4..]}";
        }

        private static string? NormalizeNullable(string? value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var normalized = value.Trim();
            return normalized.Length <= maxLength ? normalized : normalized[..maxLength];
        }

        private static ServiceResult<bool> ValidateBankAccountRequest(
            string? bankName,
            string? accountHolder,
            string? iban,
            string? currencyCode)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(bankName))
                errors.Add("Banka adı zorunludur.");

            if (string.IsNullOrWhiteSpace(accountHolder))
                errors.Add("Hesap sahibi zorunludur.");

            var normalizedIban = NormalizeIban(iban);

            if (string.IsNullOrWhiteSpace(normalizedIban))
                errors.Add("IBAN zorunludur.");
            else if (!normalizedIban.StartsWith("TR", StringComparison.OrdinalIgnoreCase) || normalizedIban.Length != 26)
                errors.Add("IBAN TR ile başlamalı ve 26 karakter olmalıdır.");

            var normalizedCurrencyCode = NormalizeCurrencyCode(currencyCode);

            if (normalizedCurrencyCode.Length != 3)
                errors.Add("Para birimi 3 karakter olmalıdır.");

            return errors.Any()
                ? ServiceResult<bool>.Fail(errors)
                : ServiceResult<bool>.Success(true);
        }

        private static string NormalizeIban(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : Regex.Replace(value.Trim().ToUpperInvariant(), "[^A-Z0-9]", "");
        }

        private static string NormalizeCurrencyCode(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? "TRY"
                : value.Trim().ToUpperInvariant();
        }

        private static object GetBankAccountAuditValues(WorkshopBankAccount account)
        {
            return new
            {
                account.Id,
                account.WorkshopId,
                account.BankName,
                account.AccountHolder,
                account.Iban,
                account.CurrencyCode,
                account.BranchName,
                account.AccountNumber,
                account.Description,
                account.IsDefault,
                account.ShowOnInvoices,
                account.ShowOnServiceForms,
                account.IsActive,
                account.SortOrder
            };
        }

        private static List<string> MapIdentityErrors(IdentityResult result)
        {
            return result.Errors
                .Select(x => x.Code switch
                {
                    "PasswordMismatch" => "Mevcut şifre hatalı.",
                    "DuplicateEmail" => "Bu e-posta adresi başka bir kullanıcıda kayıtlı.",
                    "PasswordTooShort" => "Şifre en az 6 karakter olmalıdır.",
                    _ => x.Description
                })
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();
        }
    }
}
