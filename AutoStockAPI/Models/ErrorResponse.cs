namespace AutoStock.API.Models
{
    public class ErrorResponse
    {
        public bool IsSuccess { get; set; } = false;

        public string ErrorMessage { get; set; } = null!;

        public List<string> ErrorMessages { get; set; } = new();

        public string? TraceId { get; set; }
    }
}