using AutoStock.Repositories.Enums;

namespace AutoStock.WEB.Models.SupportRequests
{
    public class AdminUpdateSupportRequestStatusViewModel
    {
        public int Id { get; set; }

        public SupportRequestStatus Status { get; set; }
    }
}