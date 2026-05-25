namespace AutoStock.Services.Dtos.Admin.Workshops
{
    public class UpdateAdminWorkshopPartnerRequestDto
    {
        public string FullName { get; set; } = null!;

        public string? Title { get; set; }

        public string? PhoneNumber { get; set; }

        public string? Email { get; set; }

        public bool IsPrimary { get; set; }

        public string? Note { get; set; }
    }
}