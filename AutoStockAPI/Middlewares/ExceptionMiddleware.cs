using AutoStock.API.Models;
using System.Net;
using System.Security.Claims;
using System.Text.Json;

namespace AutoStock.API.Middlewares
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(
            RequestDelegate next,
            IWebHostEnvironment environment,
            ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _environment = environment;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (UnauthorizedAccessException ex)
            {
                await WriteErrorResponseAsync(
                    context,
                    HttpStatusCode.Unauthorized,
                    "Bu işlem için yetkiniz bulunmuyor.",
                    ex);
            }
            catch (ArgumentException ex)
            {
                await WriteErrorResponseAsync(
                    context,
                    HttpStatusCode.BadRequest,
                    ex.Message,
                    ex);
            }
            catch (InvalidOperationException ex)
            {
                await WriteErrorResponseAsync(
                    context,
                    HttpStatusCode.BadRequest,
                    ex.Message,
                    ex);
            }
            catch (Exception ex)
            {
                await WriteErrorResponseAsync(
                    context,
                    HttpStatusCode.InternalServerError,
                    "İşlem sırasında beklenmeyen bir hata oluştu. Lütfen tekrar deneyin.",
                    ex);
            }
        }

        private async Task WriteErrorResponseAsync(
            HttpContext context,
            HttpStatusCode statusCode,
            string userMessage,
            Exception exception)
        {
            var traceId = context.TraceIdentifier;

            var userId =
                context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                context.User.FindFirst("userId")?.Value ??
                context.User.FindFirst("UserId")?.Value;

            var workshopId =
                context.User.FindFirst("workshopId")?.Value ??
                context.User.FindFirst("WorkshopId")?.Value;

            var role =
                context.User.FindFirst(ClaimTypes.Role)?.Value ??
                context.User.FindFirst("role")?.Value ??
                context.User.FindFirst("Role")?.Value;

            var ipAddress = context.Connection.RemoteIpAddress?.ToString();
            var userAgent = context.Request.Headers.UserAgent.ToString();

            var path = context.Request.Path.ToString();
            var method = context.Request.Method;
            var statusCodeNumber = (int)statusCode;

            if (statusCodeNumber >= 500)
            {
                _logger.LogError(
                    exception,
                    "API exception. TraceId: {TraceId}, StatusCode: {StatusCode}, Method: {Method}, Path: {Path}, UserId: {UserId}, Role: {Role}, WorkshopId: {WorkshopId}, IpAddress: {IpAddress}, UserAgent: {UserAgent}",
                    traceId,
                    statusCodeNumber,
                    method,
                    path,
                    userId,
                    role,
                    workshopId,
                    ipAddress,
                    userAgent);
            }
            else
            {
                _logger.LogWarning(
                    exception,
                    "API handled exception. TraceId: {TraceId}, StatusCode: {StatusCode}, Method: {Method}, Path: {Path}, UserId: {UserId}, Role: {Role}, WorkshopId: {WorkshopId}, IpAddress: {IpAddress}, UserAgent: {UserAgent}",
                    traceId,
                    statusCodeNumber,
                    method,
                    path,
                    userId,
                    role,
                    workshopId,
                    ipAddress,
                    userAgent);
            }

            if (context.Response.HasStarted)
                return;

            context.Response.Clear();
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = statusCodeNumber;

            var response = new ErrorResponse
            {
                ErrorMessage = userMessage,
                ErrorMessages = [userMessage],
                TraceId = traceId
            };

            if (_environment.IsDevelopment())
            {
                response.ErrorMessages.Add(exception.ToString());
            }

            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(json);
        }
    }
}