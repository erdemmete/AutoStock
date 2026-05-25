using System.ComponentModel.DataAnnotations;

namespace AutoStock.WEB.Models.Admin.Workshops
{
    public class CreateAdminWorkshopUserViewModel
    {
        public int WorkshopId { get; set; }

        [Required(ErrorMessage = "Ad soyad zorunludur.")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Kullanıcı adı zorunludur.")]
        public string UserName { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Şifre zorunludur.")]
        public string Password { get; set; } = string.Empty;

        public string Role { get; set; } = "Staff";
    }
}