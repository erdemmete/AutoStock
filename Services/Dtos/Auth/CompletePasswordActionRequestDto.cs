namespace AutoStock.Services.Dtos.Auth
{
    public class CompletePasswordActionRequestDto
    {
        public string Token { get; set; } = null!;

        public string NewPassword { get; set; } = null!;

        public string ConfirmNewPassword { get; set; } = null!;
    }
}