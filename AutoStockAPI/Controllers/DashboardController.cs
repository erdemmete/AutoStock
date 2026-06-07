using AutoStock.Repositories;
using AutoStock.Services.Constants;
using AutoStock.Services.Dtos.Common;
using AutoStock.Services.Dtos.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoStock.API.Controllers
{
    [Authorize(Roles = AppRoles.Admin + "," + AppRoles.Owner + "," + AppRoles.Staff)]
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : BaseApiController
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var userIdResult = GetCurrentUserId();

            if (userIdResult.IsFailure)
                return UnauthorizedResult(userIdResult);

            var role = GetCurrentUserRole() ?? AppRoles.Staff;

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == userIdResult.Data);

            if (user is null)
            {
                return Unauthorized(ServiceResult<object>.Fail(
                    "Kullanıcı bilgisi bulunamadı."));
            }

            if (!user.IsActive)
            {
                return Unauthorized(ServiceResult<object>.Fail(
                    "Kullanıcı pasif durumda."));
            }

            var response = new DashboardResponseDto
            {
                UserId = user.Id,
                FullName = user.FullName,
                Role = role,
                TodayServiceCount = 0,
                TotalCustomerCount = 0,
                PendingJobCount = 0
            };

            if (role == AppRoles.Admin)
            {
                response.WorkshopId = 0;
                response.WorkshopName = string.Empty;

                return Ok(response);
            }

            var workshopIdResult = GetCurrentWorkshopId();

            if (workshopIdResult.IsFailure)
                return UnauthorizedResult(workshopIdResult);

            var workshop = await _context.Workshops
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == workshopIdResult.Data);

            if (workshop is null)
            {
                return NotFound(ServiceResult<object>.Fail(
                    "Servis bulunamadı."));
            }

            response.WorkshopId = workshop.Id;
            response.WorkshopName = workshop.Name;

            return Ok(response);
        }
    }
}