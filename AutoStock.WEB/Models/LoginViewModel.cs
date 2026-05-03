using System.ComponentModel.DataAnnotations;

namespace AutoStock.WEB.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Kullanıcı adı veya e-posta zorunludur.")]
        public string LoginName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şifre zorunludur.")]
        public string Password { get; set; } = string.Empty;
    }
}