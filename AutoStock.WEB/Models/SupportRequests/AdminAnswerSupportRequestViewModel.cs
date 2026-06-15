using AutoStock.Repositories.Enums;

namespace AutoStock.WEB.Models.SupportRequests
{
    public class AdminAnswerSupportRequestViewModel
    {
        public int Id { get; set; }

        public string AdminResponse { get; set; } = null!;

        public SupportRequestStatus Status { get; set; } = SupportRequestStatus.Answered;
        public bool CloseAfterAnswer { get; set; }
    }
}