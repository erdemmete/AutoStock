namespace AutoStock.Services.Dtos.Auth
{
    public class ValidatePasswordActionCodeRequestDto
    {
        public string UserName { get; set; } = null!;

        public string Code { get; set; } = null!;
    }
}