using AutoStock.Repositories.Enums;

namespace AutoStock.Repositories.Entities
{
    public class WorkshopEmailRecipient
    {
        public int Id { get; set; }

        public int WorkshopId { get; set; }

        public string DisplayName { get; set; } = null!;

        public string Email { get; set; } = null!;

        public EmailRecipientType RecipientType { get; set; }

        public bool IsDefault { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime? LastUsedAt { get; set; }

        public DateTime CreatedAt { get; set; }

        public Workshop Workshop { get; set; } = null!;
    }
}
