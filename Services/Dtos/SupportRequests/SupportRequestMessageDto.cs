namespace AutoStock.Services.Dtos.SupportRequests
{
    public class SupportRequestMessageDto
    {
        public int Id { get; set; }

        public int SupportRequestId { get; set; }

        public int SenderUserId { get; set; }

        public string SenderUserName { get; set; } = null!;

        public bool IsAdminMessage { get; set; }

        public string Message { get; set; } = null!;

        public DateTime CreatedAt { get; set; }
    }
}
