namespace AutoStock.WEB.Models.SupportRequests
{
    public class SupportRequestMessageViewModel
    {
        public int Id { get; set; }
        public int SupportRequestId { get; set; }
        public int SenderUserId { get; set; }
        public string SenderUserName { get; set; } = string.Empty;
        public bool IsAdminMessage { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
