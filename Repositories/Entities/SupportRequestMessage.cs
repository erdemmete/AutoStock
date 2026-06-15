using AutoStock.Repositories.Enums;

namespace AutoStock.Repositories.Entities
{
    public class SupportRequestMessage
    {
        public int Id { get; set; }

        public int SupportRequestId { get; set; }
        public SupportRequest SupportRequest { get; set; } = null!;

        public int SenderUserId { get; set; }
        public AppUser SenderUser { get; set; } = null!;

        public bool IsAdminMessage { get; set; }

        public string Message { get; set; } = null!;

        public DateTime CreatedAt { get; set; }
    }
}