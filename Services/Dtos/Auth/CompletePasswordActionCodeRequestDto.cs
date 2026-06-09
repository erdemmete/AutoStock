namespace AutoStock.Services.Dtos.Auth
{
    public class CompletePasswordActionCodeRequestDto
    {
        public string UserName { get; set; } = null!;

        public string Code { get; set; } = null!;

        public string NewPassword { get; set; } = null!;

        public string ConfirmNewPassword { get; set; } = null!;
    }
}