using System.ComponentModel.DataAnnotations;

namespace AutoStock.WEB.Models.Auth
{
    public class PasswordSetupViewModel
    {
        [Required]
        public string Token { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string UserName { get; set; } = string.Empty;

        public string? WorkshopName { get; set; }

        public string? ErrorMessage { get; set; }

        [Required(ErrorMessage = "Yeni şifre zorunludur.")]
        [MinLength(6, ErrorMessage = "Şifre en az 6 karakter olmalıdır.")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şifre tekrarı zorunludur.")]
        [Compare(nameof(NewPassword), ErrorMessage = "Şifreler eşleşmiyor.")]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }
}