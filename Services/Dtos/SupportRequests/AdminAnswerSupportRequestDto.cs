namespace AutoStock.Services.Dtos.SupportRequests
{
    public class AdminAnswerSupportRequestDto
    {
        public int Id { get; set; }

        public string AdminResponse { get; set; } = null!;

        public bool CloseAfterAnswer { get; set; }
    }
}
