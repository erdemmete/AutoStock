using System.ComponentModel.DataAnnotations;

using AutoStock.WEB.Models.Validation;

namespace AutoStock.WEB.Models.Auth
{
    public class InviteCodeViewModel
    {
        [Required(ErrorMessage = "Kullanıcı adı zorunludur.")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Davet kodu zorunludur.")]
        public string Code { get; set; } = string.Empty;

        public string? ErrorMessage { get; set; }

        [Required(ErrorMessage = "Yeni şifre zorunludur.")]
        [MinLength(6, ErrorMessage = "Şifre en az 6 karakter olmalıdır.")]
        [PasswordPolicy]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şifre tekrarı zorunludur.")]
        [Compare(nameof(NewPassword), ErrorMessage = "Şifreler eşleşmiyor.")]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }
}
