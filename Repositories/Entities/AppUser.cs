using Microsoft.AspNetCore.Identity;

namespace AutoStock.Repositories.Entities
{
    public class AppUser : IdentityUser<int>
    {
        public string FullName { get; set; } = null!;

        public DateTime CreatedAt { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime? PasswordChangedAt { get; set; }

        public DateTime? LastPasswordResetAt { get; set; }
    }
}