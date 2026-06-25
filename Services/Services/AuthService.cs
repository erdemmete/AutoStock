using AutoStock.Repositories;
using AutoStock.Repositories.Entities;
using AutoStock.Repositories.Enums;
using AutoStock.Services.Constants;
using AutoStock.Services.Dtos.AuditLogs;
using AutoStock.Services.Dtos.Auth;
using AutoStock.Services.Dtos.Common;
using AutoStock.Services.Interfaces;
using AutoStock.Services.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace AutoStock.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly AppDbContext _context;
    private readonly JwtService _jwtService;
    private readonly IAuditLogService _auditLogService;
    private readonly IUserSecurityTokenService _userSecurityTokenService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AuthService(
    UserManager<AppUser> userManager,
    AppDbContext context,
    JwtService jwtService,
    IAuditLogService auditLogService,
    IUserSecurityTokenService userSecurityTokenService,
    IDateTimeProvider dateTimeProvider)
    {
        _userManager = userManager;
        _context = context;
        _jwtService = jwtService;
        _auditLogService = auditLogService;
        _userSecurityTokenService = userSecurityTokenService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
            throw new ArgumentException("Ad soyad zorunludur.");

        if (string.IsNullOrWhiteSpace(request.UserName))
            throw new ArgumentException("Kullanıcı adı zorunludur.");

        if (string.IsNullOrWhiteSpace(request.Password))
            throw new ArgumentException("Şifre zorunludur.");

        if (request.WorkshopId <= 0)
            throw new ArgumentException("Geçerli bir servis seçilmelidir.");

        var userName = request.UserName.Trim();

        var userNameExists = await _userManager.FindByNameAsync(userName);

        if (userNameExists is not null)
            throw new ArgumentException("Bu kullanıcı adı zaten kullanılıyor.");

        string? email = null;

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            email = request.Email.Trim();

            var emailExists = await _userManager.FindByEmailAsync(email);

            if (emailExists is not null)
                throw new ArgumentException("Bu e-posta adresi zaten kullanılıyor.");
        }

        var workshopExists = await _context.Workshops
            .AnyAsync(x => x.Id == request.WorkshopId);

        if (!workshopExists)
            throw new ArgumentException("Seçilen servis bulunamadı.");

        await using var transaction = await _context.Database.BeginTransactionAsync();

        var user = new AppUser
        {
            FullName = request.FullName.Trim(),
            UserName = userName,
            Email = email,
            IsActive = true
        };

        var createUserResult = await _userManager.CreateAsync(user, request.Password);

        if (!createUserResult.Succeeded)
        {
            await transaction.RollbackAsync();

            var errors = string.Join(" | ", createUserResult.Errors.Select(x => x.Description));
            throw new ArgumentException(errors);
        }

        var addRoleResult = await _userManager.AddToRoleAsync(user, AppRoles.Owner);

        if (!addRoleResult.Succeeded)
        {
            await transaction.RollbackAsync();

            var errors = string.Join(" | ", addRoleResult.Errors.Select(x => x.Description));
            throw new ArgumentException(errors);
        }

        var workshopUser = new WorkshopUser
        {
            UserId = user.Id,
            WorkshopId = request.WorkshopId,
            Role = AppRoles.Owner
        };

        await _context.WorkshopUsers.AddAsync(workshopUser);
        await _context.SaveChangesAsync();

        await transaction.CommitAsync();

        var token = _jwtService.GenerateToken(
            user.Id,
            user.Email ?? string.Empty,
            user.FullName,
            request.WorkshopId,
            AppRoles.Owner,
            user.SecurityStamp);

        return new AuthResponseDto
        {
            AccessToken = token,
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            WorkshopId = request.WorkshopId,
            Role = AppRoles.Owner
        };
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request)
    {
        var loginName = request.LoginName?.Trim();

        if (string.IsNullOrWhiteSpace(loginName) || string.IsNullOrWhiteSpace(request.Password))
            throw new ArgumentException("Kullanıcı adı ve şifre zorunludur.");

        AppUser? user;

        if (loginName.Contains("@"))
            user = await _userManager.FindByEmailAsync(loginName);
        else
            user = await _userManager.FindByNameAsync(loginName);

        if (user is null)
        {
            await WriteLoginFailedAuditAsync(
                loginName,
                null,
                null,
                null,
                "InvalidCredentials");

            throw new UnauthorizedAccessException("Kullanıcı adı veya şifre hatalı.");
        }

        if (!user.IsActive)
        {
            await WriteLoginFailedAuditAsync(
                loginName,
                user,
                null,
                null,
                "UserPassive");

            throw new UnauthorizedAccessException("Kullanıcı pasif durumda.");
        }

        var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);

        if (!passwordValid)
        {
            await WriteLoginFailedAuditAsync(
                loginName,
                user,
                null,
                null,
                "InvalidCredentials");

            throw new UnauthorizedAccessException("Kullanıcı adı veya şifre hatalı.");
        }

        var roles = await _userManager.GetRolesAsync(user);
        var role = GetPrimaryRole(roles);

        int workshopId = 0;

        if (role != AppRoles.Admin)
        {
            var workshopUser = await _context.WorkshopUsers
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserId == user.Id);

            if (workshopUser is null)
            {
                await WriteLoginFailedAuditAsync(
                    loginName,
                    user,
                    role,
                    null,
                    "WorkshopNotLinked");

                throw new Exception("Kullanıcı herhangi bir servise bağlı değil.");
            }

            workshopId = workshopUser.WorkshopId;
        }

        var token = _jwtService.GenerateToken(
            user.Id,
            user.Email ?? string.Empty,
            user.FullName,
            workshopId,
            role,
            user.SecurityStamp);

        int? auditWorkshopId = workshopId > 0
            ? workshopId
            : (int?)null;

        await _auditLogService.WriteAsync(new AuditLogCreateDto
        {
            WorkshopId = auditWorkshopId,
            UserId = user.Id,
            UserFullName = user.FullName,
            UserRole = role,
            ActionType = AuditActionType.LoginSuccess,
            EntityType = AuditEntityType.Auth,
            EntityId = user.Id,
            Description = $"Başarılı giriş: {user.FullName} / {role}",
            NewValues = new
            {
                LoginName = loginName,
                UserId = user.Id,
                user.FullName,
                Role = role,
                WorkshopId = auditWorkshopId,
                Success = true
            }
        });

        return new AuthResponseDto
        {
            AccessToken = token,
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            WorkshopId = workshopId,
            Role = role
        };
    }

    public async Task<ServiceResult<PasswordActionTokenInfoDto>> ValidatePasswordSetupTokenAsync(
    ValidatePasswordActionTokenRequestDto request)
    {
        return await ValidatePasswordActionTokenAsync(
            request,
            UserSecurityTokenPurpose.PasswordSetup);
    }

    public async Task<ServiceResult<AuthResponseDto>> CompletePasswordSetupAsync(CompletePasswordActionRequestDto request, string? ipAddress, string? userAgent)
    {
        return await CompletePasswordActionAsync(
            request,
            UserSecurityTokenPurpose.PasswordSetup,
            ipAddress,
            userAgent,
            "Kullanıcı davet bağlantısı ile şifre oluşturdu");
    }

    public async Task<ServiceResult<PasswordActionTokenInfoDto>> ValidatePasswordResetTokenAsync(
        ValidatePasswordActionTokenRequestDto request)
    {
        return await ValidatePasswordActionTokenAsync(
            request,
            UserSecurityTokenPurpose.PasswordReset);
    }

    public async Task<ServiceResult<AuthResponseDto>> CompletePasswordResetAsync(CompletePasswordActionRequestDto request, string? ipAddress, string? userAgent)
    {
        return await CompletePasswordActionAsync(
            request,
            UserSecurityTokenPurpose.PasswordReset,
            ipAddress,
            userAgent,
            "Kullanıcı şifre sıfırlama bağlantısı ile yeni şifre belirledi");
    }

    private async Task WriteLoginFailedAuditAsync(string loginName, AppUser? user, string? role, int? workshopId, string reason)
    {
        await _auditLogService.WriteAsync(new AuditLogCreateDto
        {
            WorkshopId = workshopId,
            UserId = user?.Id,
            UserFullName = user?.FullName,
            UserRole = role,
            ActionType = AuditActionType.LoginFailed,
            EntityType = AuditEntityType.Auth,
            EntityId = user?.Id,
            Description = $"Başarısız giriş denemesi: {loginName}",
            NewValues = new
            {
                LoginName = loginName,
                UserId = user?.Id,
                UserFullName = user?.FullName,
                Role = role,
                WorkshopId = workshopId,
                Reason = reason,
                Success = false
            }
        });
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

    private async Task<AuthResponseDto> BuildAuthResponseAsync(AppUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var role = GetPrimaryRole(roles);

        int workshopId = 0;

        if (role != AppRoles.Admin)
        {
            var workshopUser = await _context.WorkshopUsers
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserId == user.Id);

            if (workshopUser is null)
                throw new Exception("Kullanıcı herhangi bir servise bağlı değil.");

            workshopId = workshopUser.WorkshopId;
        }

        var token = _jwtService.GenerateToken(
            user.Id,
            user.Email ?? string.Empty,
            user.FullName,
            workshopId,
            role,
            user.SecurityStamp);

        return new AuthResponseDto
        {
            AccessToken = token,
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            WorkshopId = workshopId,
            Role = role
        };
    }

    private async Task<ServiceResult<PasswordActionTokenInfoDto>> ValidatePasswordActionTokenAsync(ValidatePasswordActionTokenRequestDto request, UserSecurityTokenPurpose purpose)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
            return ServiceResult<PasswordActionTokenInfoDto>.Fail("Bağlantı geçersiz.");

        var tokenResult = await _userSecurityTokenService.ValidateAsync(
            request.Token,
            purpose);

        if (tokenResult.IsFailure)
        {
            return ServiceResult<PasswordActionTokenInfoDto>.Fail(
                tokenResult.ErrorMessages,
                (HttpStatusCode)tokenResult.StatusCode);
        }

        var tokenData = tokenResult.Data!;

        var workshopInfo = await _context.WorkshopUsers
            .AsNoTracking()
            .Include(x => x.Workshop)
            .Where(x => x.UserId == tokenData.UserId)
            .Select(x => new
            {
                x.WorkshopId,
                WorkshopName = x.Workshop.Name,
                x.Role
            })
            .FirstOrDefaultAsync();

        var response = new PasswordActionTokenInfoDto
        {
            UserId = tokenData.UserId,
            FullName = tokenData.FullName,
            UserName = tokenData.UserName,
            Email = tokenData.Email,
            PhoneNumber = tokenData.PhoneNumber,
            WorkshopId = workshopInfo?.WorkshopId,
            WorkshopName = workshopInfo?.WorkshopName,
            Role = workshopInfo?.Role,
            Purpose = tokenData.Purpose,
            ExpiresAt = tokenData.ExpiresAt
        };

        return ServiceResult<PasswordActionTokenInfoDto>.Success(response);
    }

    private async Task<ServiceResult<AuthResponseDto>> CompletePasswordActionAsync(CompletePasswordActionRequestDto request, UserSecurityTokenPurpose purpose, string? ipAddress, string? userAgent, string auditDescription)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
            return ServiceResult<AuthResponseDto>.Fail("Bağlantı geçersiz.");

        if (string.IsNullOrWhiteSpace(request.NewPassword))
            return ServiceResult<AuthResponseDto>.Fail("Yeni şifre zorunludur.");

        if (request.NewPassword != request.ConfirmNewPassword)
            return ServiceResult<AuthResponseDto>.Fail("Şifreler eşleşmiyor.");

        var tokenResult = await _userSecurityTokenService.ValidateAsync(
            request.Token,
            purpose);

        if (tokenResult.IsFailure)
        {
            return ServiceResult<AuthResponseDto>.Fail(
                tokenResult.ErrorMessages,
                (HttpStatusCode)tokenResult.StatusCode);
        }

        var tokenData = tokenResult.Data!;

        var user = await _userManager.FindByIdAsync(tokenData.UserId.ToString());

        if (user is null)
            return ServiceResult<AuthResponseDto>.Fail("Kullanıcı bulunamadı.", HttpStatusCode.NotFound);

        if (!user.IsActive)
            return ServiceResult<AuthResponseDto>.Fail("Kullanıcı pasif durumda.");

        var passwordValidationResult = await ValidateNewPasswordAsync(user, request.NewPassword);
        if (passwordValidationResult.IsFailure)
            return ServiceResult<AuthResponseDto>.Fail(passwordValidationResult.ErrorMessages);

        await using var transaction = await _context.Database.BeginTransactionAsync();

        var markAsUsedResult = await _userSecurityTokenService.MarkAsUsedAsync(
            request.Token,
            purpose,
            ipAddress,
            userAgent);

        if (markAsUsedResult.IsFailure)
        {
            await transaction.RollbackAsync();

            return ServiceResult<AuthResponseDto>.Fail(
                markAsUsedResult.ErrorMessages,
                (HttpStatusCode)markAsUsedResult.StatusCode);
        }

        var hasPassword = await _userManager.HasPasswordAsync(user);

        if (hasPassword)
        {
            var removePasswordResult = await _userManager.RemovePasswordAsync(user);

            if (!removePasswordResult.Succeeded)
            {
                await transaction.RollbackAsync();

                return ServiceResult<AuthResponseDto>.Fail(MapIdentityErrors(removePasswordResult));
            }
        }

        var addPasswordResult = await _userManager.AddPasswordAsync(
            user,
            request.NewPassword);

        if (!addPasswordResult.Succeeded)
        {
            await transaction.RollbackAsync();

            return ServiceResult<AuthResponseDto>.Fail(MapIdentityErrors(addPasswordResult));
        }

        user.PasswordChangedAt = _dateTimeProvider.Now;

        if (purpose == UserSecurityTokenPurpose.PasswordReset)
            user.LastPasswordResetAt = _dateTimeProvider.Now;

        await _userManager.UpdateAsync(user);

        var authResponse = await BuildAuthResponseAsync(user);

        await _auditLogService.WriteAsync(new AuditLogCreateDto
        {
            WorkshopId = authResponse.WorkshopId > 0 ? authResponse.WorkshopId : null,
            UserId = user.Id,
            UserFullName = user.FullName,
            UserRole = authResponse.Role,
            ActionType = AuditActionType.Update,
            EntityType = AuditEntityType.Auth,
            EntityId = user.Id,
            Description = auditDescription,
            NewValues = new
            {
                UserId = user.Id,
                user.FullName,
                user.UserName,
                Purpose = purpose.ToString(),
                PasswordChangedAt = user.PasswordChangedAt,
                Success = true
            }
        });

        await _context.SaveChangesAsync();

        await transaction.CommitAsync();

        return ServiceResult<AuthResponseDto>.Success(authResponse);
    }

    public async Task<ServiceResult<PasswordActionTokenInfoDto>> ValidatePasswordSetupCodeAsync(
    ValidatePasswordActionCodeRequestDto request)
    {
        return await ValidatePasswordActionCodeAsync(
            request,
            UserSecurityTokenPurpose.PasswordSetup);
    }

    public async Task<ServiceResult<AuthResponseDto>> CompletePasswordSetupByCodeAsync(CompletePasswordActionCodeRequestDto request, string? ipAddress, string? userAgent)
    {
        return await CompletePasswordActionByCodeAsync(
            request,
            UserSecurityTokenPurpose.PasswordSetup,
            ipAddress,
            userAgent,
            "Kullanıcı davet kodu ile şifre oluşturdu");
    }

    private async Task<ServiceResult<PasswordActionTokenInfoDto>> ValidatePasswordActionCodeAsync(ValidatePasswordActionCodeRequestDto request, UserSecurityTokenPurpose purpose)
    {
        var tokenResult = await _userSecurityTokenService.ValidateByCodeAsync(
            request.UserName,
            request.Code,
            purpose);

        if (tokenResult.IsFailure)
        {
            return ServiceResult<PasswordActionTokenInfoDto>.Fail(
                tokenResult.ErrorMessages,
                (HttpStatusCode)tokenResult.StatusCode);
        }

        var tokenData = tokenResult.Data!;

        var workshopInfo = await _context.WorkshopUsers
            .AsNoTracking()
            .Include(x => x.Workshop)
            .Where(x => x.UserId == tokenData.UserId)
            .Select(x => new
            {
                x.WorkshopId,
                WorkshopName = x.Workshop.Name,
                x.Role
            })
            .FirstOrDefaultAsync();

        var response = new PasswordActionTokenInfoDto
        {
            UserId = tokenData.UserId,
            FullName = tokenData.FullName,
            UserName = tokenData.UserName,
            Email = tokenData.Email,
            PhoneNumber = tokenData.PhoneNumber,
            WorkshopId = workshopInfo?.WorkshopId,
            WorkshopName = workshopInfo?.WorkshopName,
            Role = workshopInfo?.Role,
            Purpose = tokenData.Purpose,
            ExpiresAt = tokenData.ExpiresAt
        };

        return ServiceResult<PasswordActionTokenInfoDto>.Success(response);
    }

    private async Task<ServiceResult<AuthResponseDto>> CompletePasswordActionByCodeAsync(
    CompletePasswordActionCodeRequestDto request,
    UserSecurityTokenPurpose purpose,
    string? ipAddress,
    string? userAgent,
    string auditDescription)
    {
        if (string.IsNullOrWhiteSpace(request.UserName))
            return ServiceResult<AuthResponseDto>.Fail("Kullanıcı adı zorunludur.");

        if (string.IsNullOrWhiteSpace(request.Code))
            return ServiceResult<AuthResponseDto>.Fail("Davet kodu zorunludur.");

        if (string.IsNullOrWhiteSpace(request.NewPassword))
            return ServiceResult<AuthResponseDto>.Fail("Yeni şifre zorunludur.");

        if (request.NewPassword != request.ConfirmNewPassword)
            return ServiceResult<AuthResponseDto>.Fail("Şifreler eşleşmiyor.");

        var tokenResult = await _userSecurityTokenService.ValidateByCodeAsync(
            request.UserName,
            request.Code,
            purpose);

        if (tokenResult.IsFailure)
        {
            return ServiceResult<AuthResponseDto>.Fail(
                tokenResult.ErrorMessages,
                (HttpStatusCode)tokenResult.StatusCode);
        }

        var tokenData = tokenResult.Data!;

        var user = await _userManager.FindByIdAsync(tokenData.UserId.ToString());

        if (user is null)
            return ServiceResult<AuthResponseDto>.Fail("Kullanıcı bulunamadı.", HttpStatusCode.NotFound);

        if (!user.IsActive)
            return ServiceResult<AuthResponseDto>.Fail("Kullanıcı pasif durumda.");

        var passwordValidationResult = await ValidateNewPasswordAsync(user, request.NewPassword);
        if (passwordValidationResult.IsFailure)
            return ServiceResult<AuthResponseDto>.Fail(passwordValidationResult.ErrorMessages);

        await using var transaction = await _context.Database.BeginTransactionAsync();

        var markAsUsedResult = await _userSecurityTokenService.MarkAsUsedByCodeAsync(
            request.UserName,
            request.Code,
            purpose,
            ipAddress,
            userAgent);

        if (markAsUsedResult.IsFailure)
        {
            await transaction.RollbackAsync();

            return ServiceResult<AuthResponseDto>.Fail(
                markAsUsedResult.ErrorMessages,
                (HttpStatusCode)markAsUsedResult.StatusCode);
        }

        var hasPassword = await _userManager.HasPasswordAsync(user);

        if (hasPassword)
        {
            var removePasswordResult = await _userManager.RemovePasswordAsync(user);

            if (!removePasswordResult.Succeeded)
            {
                await transaction.RollbackAsync();

                return ServiceResult<AuthResponseDto>.Fail(MapIdentityErrors(removePasswordResult));
            }
        }

        var addPasswordResult = await _userManager.AddPasswordAsync(
            user,
            request.NewPassword);

        if (!addPasswordResult.Succeeded)
        {
            await transaction.RollbackAsync();

            return ServiceResult<AuthResponseDto>.Fail(MapIdentityErrors(addPasswordResult));
        }

        user.PasswordChangedAt = _dateTimeProvider.Now;

        await _userManager.UpdateAsync(user);

        var authResponse = await BuildAuthResponseAsync(user);

        await _auditLogService.WriteAsync(new AuditLogCreateDto
        {
            WorkshopId = authResponse.WorkshopId > 0 ? authResponse.WorkshopId : null,
            UserId = user.Id,
            UserFullName = user.FullName,
            UserRole = authResponse.Role,
            ActionType = AuditActionType.Update,
            EntityType = AuditEntityType.Auth,
            EntityId = user.Id,
            Description = auditDescription,
            NewValues = new
            {
                UserId = user.Id,
                user.FullName,
                user.UserName,
                Purpose = purpose.ToString(),
                PasswordChangedAt = user.PasswordChangedAt,
                Success = true
            }
        });

        await _context.SaveChangesAsync();

        await transaction.CommitAsync();

        return ServiceResult<AuthResponseDto>.Success(authResponse);
    }

    private async Task<ServiceResult<bool>> ValidateNewPasswordAsync(AppUser user, string password)
    {
        var errors = new List<IdentityError>();

        foreach (var validator in _userManager.PasswordValidators)
        {
            var result = await validator.ValidateAsync(_userManager, user, password);
            if (!result.Succeeded)
                errors.AddRange(result.Errors);
        }

        return errors.Count == 0
            ? ServiceResult<bool>.Success(true)
            : ServiceResult<bool>.Fail(MapIdentityErrors(IdentityResult.Failed(errors.ToArray())));
    }

    private static List<string> MapIdentityErrors(IdentityResult result)
    {
        return result.Errors
            .Select(error => error.Code switch
            {
                "PasswordTooShort" => "Şifre en az 6 karakter olmalıdır.",
                "PasswordRequiresDigit" => "Şifre en az bir rakam içermelidir.",
                "PasswordRequiresUpper" => "Şifre en az bir büyük harf içermelidir.",
                "PasswordRequiresLower" => "Şifre en az bir küçük harf içermelidir.",
                "PasswordRequiresNonAlphanumeric" => "Şifre en az bir özel karakter içermelidir.",
                "PasswordRequiresUniqueChars" => "Şifre daha fazla farklı karakter içermelidir.",
                _ => "Şifre belirlenen güvenlik kurallarını karşılamıyor."
            })
            .Distinct()
            .ToList();
    }
}
