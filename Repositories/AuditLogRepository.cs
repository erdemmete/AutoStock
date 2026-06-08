using AutoStock.Repositories.Entities;
using AutoStock.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AutoStock.Repositories
{
    public class AuditLogRepository(AppDbContext context) : IAuditLogRepository
    {
        public IQueryable<AuditLog> Query()
        {
            return context.AuditLogs
                .AsNoTracking()
                .AsQueryable();
        }

        public async Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken = default)
        {
            await context.AuditLogs.AddAsync(auditLog, cancellationToken);
        }
    }
}