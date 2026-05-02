namespace AutoStock.Services.Dtos.Auth;

public class RegisterRequestDto
{
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string WorkshopName { get; set; } = null!;
}