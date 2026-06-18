using AutoStock.Repositories;
using AutoStock.Services.Constants;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

namespace AutoStock.API.Extensions;

public static class AuthExtensions
{
    public static IServiceCollection AddJwtAuth(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("Jwt");

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,

                ValidIssuer = jwtSettings["Issuer"],
                ValidAudience = jwtSettings["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtSettings["Key"]!)
                )
            };

            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = async context =>
                {
                    var principal = context.Principal;

                    if (principal is null)
                    {
                        context.Fail("Token doğrulanamadı.");
                        return;
                    }

                    var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? principal.FindFirst("userId")?.Value
                        ?? principal.FindFirst("sub")?.Value;

                    if (!int.TryParse(userIdClaim, out var userId))
                    {
                        context.Fail("Kullanıcı bilgisi bulunamadı.");
                        return;
                    }

                    var dbContext = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
                    var user = await dbContext.Users
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x => x.Id == userId);

                    if (user is null || !user.IsActive)
                    {
                        context.Fail("Kullanıcı aktif değil.");
                        return;
                    }

                    var securityStamp = principal.FindFirst("securityStamp")?.Value;

                    if (string.IsNullOrWhiteSpace(securityStamp) ||
                        !string.Equals(securityStamp, user.SecurityStamp, StringComparison.Ordinal))
                    {
                        context.Fail("Kullanıcı oturumu yenilenmeli.");
                        return;
                    }

                    var tokenRole = principal.FindFirst(ClaimTypes.Role)?.Value
                        ?? principal.FindFirst("role")?.Value;

                    if (string.IsNullOrWhiteSpace(tokenRole))
                    {
                        context.Fail("Rol bilgisi bulunamadı.");
                        return;
                    }

                    var roleNames = await dbContext.UserRoles
                        .AsNoTracking()
                        .Where(x => x.UserId == userId)
                        .Join(
                            dbContext.Roles.AsNoTracking(),
                            userRole => userRole.RoleId,
                            role => role.Id,
                            (_, role) => role.Name)
                        .ToListAsync();

                    if (!roleNames.Contains(tokenRole))
                    {
                        context.Fail("Rol bilgisi güncel değil.");
                        return;
                    }

                    if (tokenRole == AppRoles.Admin)
                        return;

                    var workshopIdClaim = principal.FindFirst("workshopId")?.Value
                        ?? principal.FindFirst("WorkshopId")?.Value;

                    if (!int.TryParse(workshopIdClaim, out var workshopId) || workshopId <= 0)
                    {
                        context.Fail("Servis bilgisi bulunamadı.");
                        return;
                    }

                    var workshopUserValid = await dbContext.WorkshopUsers
                        .AsNoTracking()
                        .AnyAsync(x =>
                            x.UserId == userId &&
                            x.WorkshopId == workshopId &&
                            x.Role == tokenRole);

                    if (!workshopUserValid)
                    {
                        context.Fail("Servis yetkisi güncel değil.");
                    }
                }
            };
        });

        return services;
    }
}
