namespace AutoStock.Services.Dtos.Admin.Workshops
{
    public class UpdateAdminWorkshopUserRequestDto
    {
        public string FullName { get; set; } = string.Empty;

        public string UserName { get; set; } = string.Empty;

        public string? Email { get; set; }

        public string? PhoneNumber { get; set; }

        public string Role { get; set; } = string.Empty;

        public bool IsActive { get; set; }
    }
}
