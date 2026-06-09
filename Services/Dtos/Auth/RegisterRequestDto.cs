namespace AutoStock.Services.Dtos.Auth
{
    public class RegisterRequestDto
    {
        public string FullName { get; set; } = string.Empty;

        public string UserName { get; set; } = string.Empty;

        public string? Email { get; set; }

        public string Password { get; set; } = string.Empty;

        public int WorkshopId { get; set; }
    }
}