namespace AutoStock.Services.Dtos.Admin.Workshops
{
    public class AdminWorkshopUserDto
    {
        public int UserId { get; set; }

        public string FullName { get; set; } = null!;

        public string UserName { get; set; } = null!;

        public string? Email { get; set; }

        public string Role { get; set; } = null!;

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}