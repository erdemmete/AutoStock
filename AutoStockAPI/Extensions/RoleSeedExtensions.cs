using AutoStock.Repositories.Entities;
using AutoStock.Services.Constants;
using Microsoft.AspNetCore.Identity;

namespace AutoStock.API.Extensions
{
    public static class RoleSeedExtensions
    {
        public static async Task SeedRolesAsync(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();

            var roleManager = scope.ServiceProvider
                .GetRequiredService<RoleManager<IdentityRole<int>>>();

            var roles = new[]
            {
                AppRoles.Admin,
                AppRoles.User
            };

            foreach (var role in roles)
            {
                var exists = await roleManager.RoleExistsAsync(role);

                if (!exists)
                {
                    await roleManager.CreateAsync(new IdentityRole<int>
                    {
                        Name = role
                    });
                }
            }
        }
    }
}
