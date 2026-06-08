using AutoStock.Repositories.Enums;

namespace AutoStock.Services.Dtos.SupportRequests
{
    public class AdminAnswerSupportRequestDto
    {
        public int Id { get; set; }

        public string AdminResponse { get; set; } = null!;

        public SupportRequestStatus Status { get; set; } = SupportRequestStatus.Answered;
    }
}