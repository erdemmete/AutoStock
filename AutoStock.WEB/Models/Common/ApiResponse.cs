namespace AutoStock.WEB.Models.Common
{
    public class ApiResponse<T>
    {
        public T? Data { get; set; }

        public string? ErrorMessage { get; set; }

        public List<string> ErrorMessages { get; set; } = new();

        public bool IsSuccess { get; set; }

        public bool IsFailure => !IsSuccess;

        public int StatusCode { get; set; }

        public string? TraceId { get; set; }

        public static ApiResponse<T> Success(T? data, int statusCode = 200)
        {
            return new ApiResponse<T>
            {
                Data = data,
                IsSuccess = true,
                StatusCode = statusCode
            };
        }

        public static ApiResponse<T> Fail(
            string errorMessage,
            int statusCode = 0,
            string? traceId = null)
        {
            return new ApiResponse<T>
            {
                IsSuccess = false,
                ErrorMessage = errorMessage,
                ErrorMessages = new List<string> { errorMessage },
                StatusCode = statusCode,
                TraceId = traceId
            };
        }
    }
}