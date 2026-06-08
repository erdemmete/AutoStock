using AutoStock.Repositories.Enums;

namespace AutoStock.Services.Dtos.SupportRequests
{
    public class AdminUpdateSupportRequestStatusDto
    {
        public int Id { get; set; }

        public SupportRequestStatus Status { get; set; }
    }
}