using AutoStock.Repositories;
using AutoStock.Repositories.Enums;
using AutoStock.Services.Constants;
using AutoStock.Services.Dtos.Common;
using AutoStock.Services.Dtos.Dashboard;
using AutoStock.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace AutoStock.API.Controllers
{
    [Authorize(Roles = "Admin,Owner,Staff")]
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : BaseApiController
    {
        private readonly AppDbContext _context;
        private readonly IDateTimeProvider _dateTimeProvider;

        public DashboardController(
            AppDbContext context,
            IDateTimeProvider dateTimeProvider)
        {
            _context = context;
            _dateTimeProvider = dateTimeProvider;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var userIdResult = GetCurrentUserId();

            if (userIdResult.IsFailure)
                return UnauthorizedResult(userIdResult);

            var role = GetCurrentUserRole() ?? "Staff";

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == userIdResult.Data);

            if (user is null)
            {
                return Unauthorized(ServiceResult<DashboardResponseDto>.Fail(
                    "Kullanıcı bilgisi bulunamadı.",
                    HttpStatusCode.Unauthorized));
            }

            if (!user.IsActive)
            {
                return Unauthorized(ServiceResult<DashboardResponseDto>.Fail(
                    "Kullanıcı pasif durumda.",
                    HttpStatusCode.Unauthorized));
            }

            var response = new DashboardResponseDto
            {
                UserId = user.Id,
                FullName = user.FullName,
                Role = role
            };

            if (role == "Admin")
            {
                return ToActionResult(ServiceResult<DashboardResponseDto>.Success(response));
            }

            var workshopIdResult = GetCurrentWorkshopId();

            if (workshopIdResult.IsFailure)
                return UnauthorizedResult(workshopIdResult);

            var workshopId = workshopIdResult.Data;

            var workshop = await _context.Workshops
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == workshopId);

            if (workshop is null)
            {
                return NotFound(ServiceResult<DashboardResponseDto>.Fail(
                    "Servis bulunamadı.",
                    HttpStatusCode.NotFound));
            }

            var todayStartUtc = _dateTimeProvider.TodayStartUtc;
            var tomorrowStartUtc = _dateTimeProvider.TomorrowStartUtc;

            var activeServiceRecordCount = await _context.ServiceRecords
                .AsNoTracking()
                .CountAsync(x =>
                    x.WorkshopId == workshopId &&
                    (x.Status == ServiceRecordStatus.Open ||
                     x.Status == ServiceRecordStatus.InProgress));

            var todayServiceRecordCount = await _context.ServiceRecords
                .AsNoTracking()
                .CountAsync(x =>
                    x.WorkshopId == workshopId &&
                    x.CreatedAt >= todayStartUtc &&
                    x.CreatedAt < tomorrowStartUtc);

            var inProgressServiceRecordCount = await _context.ServiceRecords
                .AsNoTracking()
                .CountAsync(x =>
                    x.WorkshopId == workshopId &&
                    x.Status == ServiceRecordStatus.InProgress);

            var draftInvoiceCount = await _context.Invoices
                .AsNoTracking()
                .CountAsync(x =>
                    x.WorkshopId == workshopId &&
                    x.Status == InvoiceStatus.Draft);

            var criticalStockItemCount = await _context.StockItems
                .AsNoTracking()
                .CountAsync(x =>
                    x.WorkshopId == workshopId &&
                    x.IsActive &&
                    x.MinimumQuantity > 0 &&
                    x.Quantity <= x.MinimumQuantity);

            decimal pendingCollectionAmount = 0;

            if (role == "Owner")
            {
                var debitTotal = await _context.CurrentAccountTransactions
                    .AsNoTracking()
                    .Where(x => x.WorkshopId == workshopId)
                    .SumAsync(x => (decimal?)x.Debit) ?? 0;

                var creditTotal = await _context.CurrentAccountTransactions
                    .AsNoTracking()
                    .Where(x => x.WorkshopId == workshopId)
                    .SumAsync(x => (decimal?)x.Credit) ?? 0;

                pendingCollectionAmount = Math.Max(0, debitTotal - creditTotal);
            }

            var recentServiceRecords = await _context.ServiceRecords
                .AsNoTracking()
                .Where(x => x.WorkshopId == workshopId)
                .OrderByDescending(x => x.CreatedAt)
                .Take(5)
                .Select(x => new
                {
                    x.RecordNumber,
                    x.VehiclePlateSnapshot,
                    x.CustomerNameSnapshot,
                    x.Status,
                    x.CreatedAt
                })
                .ToListAsync();

            var recentInvoices = await _context.Invoices
                .AsNoTracking()
                .Where(x => x.WorkshopId == workshopId)
                .OrderByDescending(x => x.CreatedAt)
                .Take(3)
                .Select(x => new
                {
                    x.InvoiceNumber,
                    x.CustomerTitle,
                    x.Status,
                    x.CreatedAt
                })
                .ToListAsync();

            var recentActivities = new List<DashboardActivityDto>();

            recentActivities.AddRange(recentServiceRecords.Select(x => new DashboardActivityDto
            {
                Type = "service",
                Title = $"{x.VehiclePlateSnapshot} plakalı araç için servis kaydı açıldı.",
                Description = $"{x.CustomerNameSnapshot} · {x.RecordNumber}",
                CreatedAt = x.CreatedAt
            }));

            recentActivities.AddRange(recentInvoices.Select(x => new DashboardActivityDto
            {
                Type = "invoice",
                Title = $"{x.InvoiceNumber} numaralı servis hesap özeti taslağı oluşturuldu.",
                Description = x.CustomerTitle,
                CreatedAt = x.CreatedAt
            }));

            response.WorkshopId = workshop.Id;
            response.WorkshopName = workshop.Name;
            response.ActiveServiceRecordCount = activeServiceRecordCount;
            response.TodayServiceRecordCount = todayServiceRecordCount;
            response.InProgressServiceRecordCount = inProgressServiceRecordCount;
            response.DraftInvoiceCount = draftInvoiceCount;
            response.PendingCollectionAmount = pendingCollectionAmount;
            response.CriticalStockItemCount = criticalStockItemCount;
            response.RecentActivities = recentActivities
                .OrderByDescending(x => x.CreatedAt)
                .Take(6)
                .ToList();

            return ToActionResult(ServiceResult<DashboardResponseDto>.Success(response));
        }
    }
}
