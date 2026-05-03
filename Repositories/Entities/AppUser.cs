using Microsoft.AspNetCore.Identity;

namespace AutoStock.Repositories.Entities
{
    public class AppUser : IdentityUser<int>
    {
        public string FullName { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
    }
}