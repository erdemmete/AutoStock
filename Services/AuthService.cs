using AutoStock.Repositories;
using AutoStock.Repositories.Entities;
using AutoStock.Services.Constants;
using AutoStock.Services.Dtos.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AutoStock.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly AppDbContext _context;

    private readonly JwtService _jwtService;

    public AuthService(UserManager<AppUser> userManager, AppDbContext context, JwtService jwtService)
    {
        _userManager = userManager;
        _context = context;
        _jwtService = jwtService;
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

        var user = new AppUser
        {
            FullName = request.FullName.Trim(),
            UserName = userName,
            Email = email
        };

        var createUserResult = await _userManager.CreateAsync(user, request.Password);

        if (!createUserResult.Succeeded)
        {
            var errors = string.Join(" | ", createUserResult.Errors.Select(x => x.Description));
            throw new ArgumentException(errors);
        }

        var workshopUser = new WorkshopUser
        {
            UserId = user.Id,
            WorkshopId = request.WorkshopId
        };

        await _userManager.AddToRoleAsync(user, AppRoles.User);

        await _context.WorkshopUsers.AddAsync(workshopUser);
        await _context.SaveChangesAsync();

        var token = _jwtService.GenerateToken(
    user.Id,
    user.Email ?? string.Empty,
    user.FullName,
    request.WorkshopId,
    AppRoles.User);

        return new AuthResponseDto
        {
            AccessToken = token,
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            WorkshopId = request.WorkshopId
        };
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request)
    {
        var loginName = request.LoginName?.Trim();

        if (string.IsNullOrWhiteSpace(loginName) || string.IsNullOrWhiteSpace(request.Password))
            throw new ArgumentException("Kullanıcı adı/e-posta ve şifre zorunludur.");

        // Kullanıcıyı bul (username veya email)
        AppUser? user;

        if (loginName.Contains("@"))
            user = await _userManager.FindByEmailAsync(loginName);
        else
            user = await _userManager.FindByNameAsync(loginName);

        if (user is null)
            throw new UnauthorizedAccessException("Kullanıcı adı/e-posta veya şifre hatalı.");

        // Aktif mi kontrol
        if (!user.IsActive)
            throw new UnauthorizedAccessException("Kullanıcı pasif durumda.");

        // Şifre kontrolü
        var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);

        if (!passwordValid)
            throw new UnauthorizedAccessException("Kullanıcı adı/e-posta veya şifre hatalı.");

        // Workshop bağlantısı
        var workshopUser = await _context.WorkshopUsers
            .FirstOrDefaultAsync(x => x.UserId == user.Id);

        if (workshopUser is null)
            throw new Exception("Kullanıcı herhangi bir servise bağlı değil.");

        // Role al (Identity kullanıyorsan)
        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? "User";

        // Token üret
        var token = _jwtService.GenerateToken(
            user.Id,
            user.Email ?? string.Empty,
            user.FullName,
            workshopUser.WorkshopId,
            role
        );

        return new AuthResponseDto
        {
            AccessToken = token,
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            WorkshopId = workshopUser.WorkshopId
        };
    }
}