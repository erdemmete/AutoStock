using AutoStock.Repositories.Entities;
using AutoStock.Repositories.Enums;
using AutoStock.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AutoStock.Repositories.Repositories
{
    public class SupportRequestRepository : ISupportRequestRepository
    {
        private readonly AppDbContext _context;

        public SupportRequestRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<SupportRequest?> GetByIdAsync(int id)
        {
            return await _context.SupportRequests
                .Include(x => x.Workshop)
                .Include(x => x.CreatedByUser)
                .Include(x => x.RespondedByUser)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<SupportRequest?> GetByIdForWorkshopAsync(
    int id,
    int workshopId,
    int? createdByUserId = null)
        {
            var query = _context.SupportRequests
                .Include(x => x.Workshop)
                .Include(x => x.CreatedByUser)
                .Include(x => x.RespondedByUser)
                .Where(x =>
                    x.Id == id &&
                    x.WorkshopId == workshopId);

            if (createdByUserId.HasValue)
            {
                query = query.Where(x => x.CreatedByUserId == createdByUserId.Value);
            }

            return await query.FirstOrDefaultAsync();
        }

        public async Task<List<SupportRequest>> GetListForWorkshopAsync(
     int workshopId,
     SupportRequestStatus? status,
     SupportRequestType? requestType,
     string? search,
     DateTime? startDate,
     DateTime? endDate,
     int page,
     int pageSize,
     int? createdByUserId = null,
     bool excludeClosedAndCancelled = false)
        {
            var query = _context.SupportRequests
                .AsNoTracking()
                .Include(x => x.Workshop)
                .Include(x => x.CreatedByUser)
                .Include(x => x.RespondedByUser)
                .Where(x => x.WorkshopId == workshopId);

            query = ApplyWorkshopVisibilityFilters(
                query,
                createdByUserId,
                excludeClosedAndCancelled);

            query = ApplyCommonFilters(
                query,
                status,
                requestType,
                search,
                startDate,
                endDate);

            return await query
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetCountForWorkshopAsync(
    int workshopId,
    SupportRequestStatus? status,
    SupportRequestType? requestType,
    string? search,
    DateTime? startDate,
    DateTime? endDate,
    int? createdByUserId = null,
    bool excludeClosedAndCancelled = false)
        {
            var query = _context.SupportRequests
                .AsNoTracking()
                .Include(x => x.Workshop)
                .Where(x => x.WorkshopId == workshopId);

            query = ApplyWorkshopVisibilityFilters(
                query,
                createdByUserId,
                excludeClosedAndCancelled);

            query = ApplyCommonFilters(
                query,
                status,
                requestType,
                search,
                startDate,
                endDate);

            return await query.CountAsync();
        }

        public async Task<List<SupportRequest>> GetListForAdminAsync(
            int? workshopId,
            SupportRequestStatus? status,
            SupportRequestType? requestType,
            string? search,
            DateTime? startDate,
            DateTime? endDate,
            int page,
            int pageSize)
        {
            var query = _context.SupportRequests
                .AsNoTracking()
                .Include(x => x.Workshop)
                .Include(x => x.CreatedByUser)
                .Include(x => x.RespondedByUser)
                .AsQueryable();

            if (workshopId.HasValue)
            {
                query = query.Where(x => x.WorkshopId == workshopId.Value);
            }

            query = ApplyCommonFilters(query, status, requestType, search, startDate, endDate);

            return await query
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetCountForAdminAsync(
            int? workshopId,
            SupportRequestStatus? status,
            SupportRequestType? requestType,
            string? search,
            DateTime? startDate,
            DateTime? endDate)
        {
            var query = _context.SupportRequests
                .AsNoTracking()
                .Include(x => x.Workshop)
                .AsQueryable();

            if (workshopId.HasValue)
            {
                query = query.Where(x => x.WorkshopId == workshopId.Value);
            }

            query = ApplyCommonFilters(query, status, requestType, search, startDate, endDate);

            return await query.CountAsync();
        }

        public async Task AddAsync(SupportRequest supportRequest)
        {
            await _context.SupportRequests.AddAsync(supportRequest);
        }

        public void Update(SupportRequest supportRequest)
        {
            _context.SupportRequests.Update(supportRequest);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        private static IQueryable<SupportRequest> ApplyCommonFilters(
            IQueryable<SupportRequest> query,
            SupportRequestStatus? status,
            SupportRequestType? requestType,
            string? search,
            DateTime? startDate,
            DateTime? endDate)
        {
            if (status.HasValue)
            {
                query = query.Where(x => x.Status == status.Value);
            }

            if (requestType.HasValue)
            {
                query = query.Where(x => x.RequestType == requestType.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var normalizedSearch = search.Trim();

                query = query.Where(x =>
                    x.Subject.Contains(normalizedSearch) ||
                    x.Description.Contains(normalizedSearch) ||
                    (x.AdminResponse != null && x.AdminResponse.Contains(normalizedSearch)) ||
                    (x.RequestedUserFullName != null && x.RequestedUserFullName.Contains(normalizedSearch)) ||
                    (x.RequestedUserPhone != null && x.RequestedUserPhone.Contains(normalizedSearch)) ||
                    (x.RequestedUserEmail != null && x.RequestedUserEmail.Contains(normalizedSearch)) ||
                    (x.Workshop != null && x.Workshop.Name.Contains(normalizedSearch)));
            }

            if (startDate.HasValue)
            {
                query = query.Where(x => x.CreatedAt >= startDate.Value.Date);
            }

            if (endDate.HasValue)
            {
                var exclusiveEndDate = endDate.Value.Date.AddDays(1);
                query = query.Where(x => x.CreatedAt < exclusiveEndDate);
            }

            return query;
        }

        private static IQueryable<SupportRequest> ApplyWorkshopVisibilityFilters(
    IQueryable<SupportRequest> query,
    int? createdByUserId,
    bool excludeClosedAndCancelled)
        {
            if (createdByUserId.HasValue)
            {
                query = query.Where(x => x.CreatedByUserId == createdByUserId.Value);
            }

            if (excludeClosedAndCancelled)
            {
                query = query.Where(x =>
                    x.Status != SupportRequestStatus.Closed &&
                    x.Status != SupportRequestStatus.Cancelled);
            }

            return query;
        }
    }
}