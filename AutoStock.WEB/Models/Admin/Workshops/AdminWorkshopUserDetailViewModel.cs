using System.ComponentModel.DataAnnotations;

namespace AutoStock.WEB.Models.Admin.Workshops
{
    public class AdminWorkshopUserDetailViewModel
    {
        public int WorkshopId { get; set; }

        public string WorkshopName { get; set; } = string.Empty;

        public int UserId { get; set; }

        [Required(ErrorMessage = "Ad soyad zorunludur.")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Kullanıcı adı zorunludur.")]
        public string UserName { get; set; } = string.Empty;

        public string? Email { get; set; }

        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Rol zorunludur.")]
        public string Role { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? LastLoginAt { get; set; }

        public string? LastLoginIpAddress { get; set; }
    }
}
