using AutoStock.Services.Dtos.AuditLogs;
using AutoStock.Services.Interfaces;
using Serilog.Context;
using System.Security.Claims;

namespace AutoStock.API.Middlewares
{
    public class AuditContextMiddleware
    {
        private readonly RequestDelegate _next;

        public AuditContextMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(
            HttpContext httpContext,
            IAuditContextAccessor auditContextAccessor)
        {
            try
            {
                auditContextAccessor.Current = new AuditContextDto
                {
                    UserId = TryParseInt(httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)),
                    UserFullName = httpContext.User.FindFirstValue(ClaimTypes.Name),
                    UserRole = httpContext.User.FindFirstValue(ClaimTypes.Role),
                    WorkshopId = TryParseInt(httpContext.User.FindFirstValue("workshopId")),
                    IpAddress = GetIpAddress(httpContext),
                    UserAgent = httpContext.Request.Headers["User-Agent"].ToString()
                };
            }
            catch
            {
                auditContextAccessor.Current = new AuditContextDto();
            }

            using (LogContext.PushProperty("TraceId", httpContext.TraceIdentifier))
            {
                await _next(httpContext);
            }
        }

        private static int? TryParseInt(string? value)
        {
            return int.TryParse(value, out var result)
                ? result
                : null;
        }

        private static string? GetIpAddress(HttpContext httpContext)
        {
            var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(forwardedFor))
                return forwardedFor.Split(',')[0].Trim();

            return httpContext.Connection.RemoteIpAddress?.ToString();
        }
    }
}
