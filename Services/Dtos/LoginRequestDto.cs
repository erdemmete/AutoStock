namespace AutoStock.Services.Dtos.Auth
{
    public class LoginRequestDto
    {
        public string LoginName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}