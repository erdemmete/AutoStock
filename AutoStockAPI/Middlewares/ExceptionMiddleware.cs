using System.Net;
using System.Text.Json;

namespace AutoStock.API.Middlewares
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IWebHostEnvironment _environment;

        public ExceptionMiddleware(RequestDelegate next, IWebHostEnvironment environment)
        {
            _next = next;
            _environment = environment;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (UnauthorizedAccessException ex)
            {
                await WriteErrorResponse(context, HttpStatusCode.Unauthorized, ex.Message, ex);
            }
            catch (ArgumentException ex)
            {
                await WriteErrorResponse(context, HttpStatusCode.BadRequest, ex.Message, ex);
            }
            catch (Exception ex)
            {
                await WriteErrorResponse(context, HttpStatusCode.InternalServerError, "Sunucu hatası oluştu.", ex);
            }
        }

        private async Task WriteErrorResponse(
            HttpContext context,
            HttpStatusCode statusCode,
            string message,
            Exception exception)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;

            var shouldShowDetail =
                 _environment.IsDevelopment() &&
                  statusCode == HttpStatusCode.InternalServerError;

            var response = new
            {
                statusCode = context.Response.StatusCode,
                message,
                detail = shouldShowDetail ? exception.ToString() : null
            };

            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(json);
        }
    }
}