using System.Net;

namespace AutoStock.Services.Dtos.Common
{
    public class ServiceResult<T>
    {
        public T? Data { get; set; }
        public List<string>? ErrorMessage { get; set; }

        public bool IsSuccess => ErrorMessage == null || ErrorMessage.Count == 0;

        public bool IsFailure => !IsSuccess;

        public HttpStatusCode StatusCode { get; set; }

        public static ServiceResult<T> Success(T? data, HttpStatusCode statusCode = HttpStatusCode.OK)

        {
            return new ServiceResult<T> { Data = data, StatusCode = statusCode };
        }
        public static ServiceResult<T> Fail(string errorMessage, HttpStatusCode statusCode = HttpStatusCode.BadRequest)
        {
            return new ServiceResult<T> { ErrorMessage = [errorMessage], StatusCode = statusCode };
        }

        public static ServiceResult<T> Fail(List<string> errorMessages, HttpStatusCode statusCode = HttpStatusCode.BadRequest)
        {
            return new ServiceResult<T> { ErrorMessage = errorMessages, StatusCode = statusCode };
        }

    }
}
