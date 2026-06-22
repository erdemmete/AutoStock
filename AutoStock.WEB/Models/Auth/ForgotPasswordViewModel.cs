using System.ComponentModel.DataAnnotations;

namespace AutoStock.WEB.Models.Auth
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Kullanıcı adı zorunludur.")]
        public string UserName { get; set; } = string.Empty;

        public bool IsCompleted { get; set; }

        public string? InfoMessage { get; set; }
    }
}
