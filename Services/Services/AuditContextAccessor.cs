using AutoStock.Services.Dtos.AuditLogs;
using AutoStock.Services.Interfaces;

namespace AutoStock.Services.Services
{
    public class AuditContextAccessor : IAuditContextAccessor
    {
        public AuditContextDto Current { get; set; } = new();
    }
}