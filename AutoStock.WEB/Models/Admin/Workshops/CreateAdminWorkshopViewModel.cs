using System.ComponentModel.DataAnnotations;

namespace AutoStock.WEB.Models.Admin.Workshops
{
    public class CreateAdminWorkshopViewModel
    {
        [Required(ErrorMessage = "Servis adı zorunludur.")]
        public string WorkshopName { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public int SubscriptionStatus { get; set; } = 1;

        public int? TrialDays { get; set; } = 15;

        public DateTime? SubscriptionEndDate { get; set; }

        public string? SubscriptionNote { get; set; }

        [Required(ErrorMessage = "İlk kullanıcı adı soyadı zorunludur.")]
        public string FirstUserFullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Kullanıcı adı zorunludur.")]
        public string FirstUserName { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        public string? FirstUserEmail { get; set; }

        [Required(ErrorMessage = "Şifre zorunludur.")]
        public string FirstUserPassword { get; set; } = string.Empty;

        public string FirstUserRole { get; set; } = "Owner";
    }
}