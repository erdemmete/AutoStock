namespace AutoStock.Services.Dtos.Admin.Workshops
{
    public class AdminWorkshopUserDetailDto
    {
        public int WorkshopId { get; set; }

        public string WorkshopName { get; set; } = string.Empty;

        public int UserId { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string UserName { get; set; } = string.Empty;

        public string? Email { get; set; }

        public string? PhoneNumber { get; set; }

        public string Role { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? LastLoginAt { get; set; }

        public string? LastLoginIpAddress { get; set; }
    }
}
