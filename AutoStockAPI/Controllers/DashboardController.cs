using AutoStock.Repositories;

using AutoStock.Services.Dtos.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AutoStock.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var userIdValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "User";

            if (string.IsNullOrWhiteSpace(userIdValue) || !int.TryParse(userIdValue, out var userId))
                return Unauthorized();

            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.Id == userId);

            if (user is null)
                return Unauthorized();

            var response = new DashboardResponseDto
            {
                UserId = user.Id,
                FullName = user.FullName,
                Role = role,
                TodayServiceCount = 0,
                TotalCustomerCount = 0,
                PendingJobCount = 0
            };

            if (role == "Admin")
            {
                response.WorkshopId = 0;
                response.WorkshopName = string.Empty;

                return Ok(response);
            }

            var workshopIdValue = User.FindFirst("workshopId")?.Value;

            if (string.IsNullOrWhiteSpace(workshopIdValue) || !int.TryParse(workshopIdValue, out var workshopId))
                return Unauthorized();

            var workshop = await _context.Workshops
                .FirstOrDefaultAsync(x => x.Id == workshopId);

            if (workshop is null)
                return NotFound("Servis bulunamadı.");

            response.WorkshopId = workshop.Id;
            response.WorkshopName = workshop.Name;

            return Ok(response);
        }
    }
}