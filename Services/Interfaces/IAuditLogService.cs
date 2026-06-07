using AutoStock.Services.Dtos.AuditLogs;

namespace AutoStock.Services.Interfaces
{
    public interface IAuditLogService
    {
        Task AddAsync(AuditLogCreateDto request, CancellationToken cancellationToken = default);

        Task WriteAsync(AuditLogCreateDto request, CancellationToken cancellationToken = default);
    }
}