using System.ComponentModel.DataAnnotations;

namespace AutoStock.WEB.Models.Admin.Workshops
{
    public class CreateAdminWorkshopPartnerViewModel
    {
        public int WorkshopId { get; set; }

        [Required(ErrorMessage = "Ad soyad zorunludur.")]
        public string FullName { get; set; } = string.Empty;

        public string? Title { get; set; }

        public string? PhoneNumber { get; set; }

        public string? Email { get; set; }

        public bool IsPrimary { get; set; }

        public string? Note { get; set; }
    }
}