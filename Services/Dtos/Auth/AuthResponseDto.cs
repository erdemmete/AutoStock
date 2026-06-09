namespace AutoStock.Services.Dtos.Auth;

public class AuthResponseDto
{
    public string AccessToken { get; set; } = null!;
    public int UserId { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public int WorkshopId { get; set; }
    public string Role { get; set; } = string.Empty;
}