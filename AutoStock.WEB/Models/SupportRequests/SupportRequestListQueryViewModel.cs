using AutoStock.Repositories.Enums;

namespace AutoStock.WEB.Models.SupportRequests
{
    public class SupportRequestListQueryViewModel
    {
        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 10;

        public SupportRequestStatus? Status { get; set; }

        public SupportRequestType? RequestType { get; set; }

        public string? Search { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }
    }
}