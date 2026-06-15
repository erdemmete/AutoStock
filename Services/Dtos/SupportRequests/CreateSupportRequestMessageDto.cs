namespace AutoStock.Services.Dtos.SupportRequests
{
    public class CreateSupportRequestMessageDto
    {
        public int Id { get; set; }

        public string Message { get; set; } = null!;
    }
}
