using AutoStock.Repositories;
using AutoStock.Repositories.Entities;
using AutoStock.Repositories.Enums;
using AutoStock.Services.Constants;
using AutoStock.Services.Dtos.AuditLogs;
using AutoStock.Services.Dtos.Auth;
using AutoStock.Services.Interfaces;
using AutoStock.Services.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AutoStock.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly AppDbContext _context;
    private readonly JwtService _jwtService;
    private readonly IAuditLogService _auditLogService;

    public AuthService(
        UserManager<AppUser> userManager,
        AppDbContext context,
        JwtService jwtService,
        IAuditLogService auditLogService)
    {
        _userManager = userManager;
        _context = context;
        _jwtService = jwtService;
        _auditLogService = auditLogService;
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
            AppRoles.Owner);

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
            throw new ArgumentException("Kullanıcı adı/e-posta ve şifre zorunludur.");

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

            throw new UnauthorizedAccessException("Kullanıcı adı/e-posta veya şifre hatalı.");
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

            throw new UnauthorizedAccessException("Kullanıcı adı/e-posta veya şifre hatalı.");
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
            role);

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

    private async Task WriteLoginFailedAuditAsync(
        string loginName,
        AppUser? user,
        string? role,
        int? workshopId,
        string reason)
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
}