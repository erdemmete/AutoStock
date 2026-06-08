using AutoStock.Repositories.Enums;

namespace AutoStock.Services.Dtos.SupportRequests
{
    public class AdminSupportRequestListQueryDto : SupportRequestListQueryDto
    {
        public int? WorkshopId { get; set; }
    }
}