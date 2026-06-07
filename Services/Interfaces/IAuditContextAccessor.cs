using AutoStock.Services.Dtos.AuditLogs;

namespace AutoStock.Services.Interfaces
{
    public interface IAuditContextAccessor
    {
        AuditContextDto Current { get; set; }
    }
}