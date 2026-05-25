namespace AutoStock.Services.Dtos.Admin.Workshops
{
    public class CreateAdminWorkshopUserRequestDto
    {
        public string FullName { get; set; } = null!;

        public string UserName { get; set; } = null!;

        public string? Email { get; set; }

        public string Password { get; set; } = null!;

        // Sadece Owner veya Staff kabul edeceğiz.
        public string Role { get; set; } = "Staff";
    }
}