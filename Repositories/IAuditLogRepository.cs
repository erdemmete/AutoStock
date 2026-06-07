using AutoStock.Repositories.Entities;

namespace AutoStock.Repositories
{
    public interface IAuditLogRepository
    {
        IQueryable<AuditLog> Query();

        Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken = default);
    }
}