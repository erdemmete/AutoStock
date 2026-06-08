using AutoStock.Repositories.Entities;

namespace AutoStock.Repositories.Interfaces
{
    public interface IAuditLogRepository
    {
        IQueryable<AuditLog> Query();

        Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken = default);
    }
}