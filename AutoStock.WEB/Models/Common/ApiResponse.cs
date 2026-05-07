namespace AutoStock.WEB.Models.Common
{
    public class ApiResponse<T>
    {
        public T? Data { get; set; }

        public string? ErrorMessage { get; set; }

        public bool IsSuccess { get; set; }

        public bool IsFailure { get; set; }

        public int StatusCode { get; set; }
    }
}
