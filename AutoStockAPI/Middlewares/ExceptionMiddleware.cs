using AutoStock.API.Models;
using System.Net;
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

            _logger.LogError(
                exception,
                "Unhandled API exception. TraceId: {TraceId}, Path: {Path}, Method: {Method}",
                traceId,
                context.Request.Path,
                context.Request.Method);

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;

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