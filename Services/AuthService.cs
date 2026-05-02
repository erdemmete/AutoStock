using AutoStock.Repositories;
using AutoStock.Repositories.Entities;
using AutoStock.Services.Dtos.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AutoStock.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly AppDbContext _context;

    public AuthService(UserManager<AppUser> userManager, AppDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request)
    {
        var user = new AppUser
        {
            FullName = request.FullName,
            UserName = request.Email,
            Email = request.Email
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
            throw new Exception(string.Join(" | ", result.Errors.Select(x => x.Description)));

        var workshop = new Workshop
        {
            Name = request.WorkshopName
        };

        _context.Workshops.Add(workshop);
        await _context.SaveChangesAsync();

        var workshopUser = new WorkshopUser
        {
            UserId = user.Id,
            WorkshopId = workshop.Id,
            Role = "Owner"
        };

        _context.WorkshopUsers.Add(workshopUser);
        await _context.SaveChangesAsync();

        return new AuthResponseDto
        {
            AccessToken = "TEMP_TOKEN",
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email!,
            WorkshopId = workshop.Id
        };
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user is null)
            throw new Exception("Email veya şifre hatalı.");

        var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);

        if (!passwordValid)
            throw new Exception("Email veya şifre hatalı.");

        var workshopUser = await _context.WorkshopUsers
            .FirstOrDefaultAsync(x => x.UserId == user.Id);

        if (workshopUser is null)
            throw new Exception("Kullanıcı herhangi bir servise bağlı değil.");

        return new AuthResponseDto
        {
            AccessToken = "TEMP_TOKEN",
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email!,
            WorkshopId = workshopUser.WorkshopId
        };
    }
}