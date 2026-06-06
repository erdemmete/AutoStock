using System.Net;

namespace AutoStock.Services.Dtos.Common
{
    public class ServiceResult<T>
    {
        public T? Data { get; set; }

        public bool IsSuccess { get; set; }

        public bool IsFailure => !IsSuccess;

        public string? ErrorMessage { get; set; }

        public List<string> ErrorMessages { get; set; } = new();

        public int StatusCode { get; set; }

        public static ServiceResult<T> Success(
            T? data,
            HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            return new ServiceResult<T>
            {
                Data = data,
                IsSuccess = true,
                StatusCode = (int)statusCode
            };
        }

        public static ServiceResult<T> Fail(
            string? errorMessage,
            HttpStatusCode statusCode = HttpStatusCode.BadRequest)
        {
            var safeMessage = string.IsNullOrWhiteSpace(errorMessage)
                ? "İşlem sırasında bir hata oluştu."
                : errorMessage;

            return new ServiceResult<T>
            {
                IsSuccess = false,
                ErrorMessage = safeMessage,
                ErrorMessages = [safeMessage],
                StatusCode = (int)statusCode
            };
        }

        public static ServiceResult<T> Fail(
            List<string>? errorMessages,
            HttpStatusCode statusCode = HttpStatusCode.BadRequest)
        {
            var safeMessages = errorMessages is { Count: > 0 }
                ? errorMessages
                : ["İşlem sırasında bir hata oluştu."];

            return new ServiceResult<T>
            {
                IsSuccess = false,
                ErrorMessage = safeMessages.FirstOrDefault(),
                ErrorMessages = safeMessages,
                StatusCode = (int)statusCode
            };
        }
    }
}