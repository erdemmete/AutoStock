using AutoStock.Repositories.Enums;

namespace AutoStock.Services.Dtos.SupportRequests
{
    public class SupportRequestListQueryDto
    {
        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 10;

        public SupportRequestStatus? Status { get; set; }

        public SupportRequestType? RequestType { get; set; }

        public string? Search { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public void Normalize()
        {
            if (PageNumber < 1)
                PageNumber = 1;

            if (PageSize != 10 && PageSize != 25 && PageSize != 50)
                PageSize = 10;

            Search = string.IsNullOrWhiteSpace(Search)
                ? null
                : Search.Trim();
        }
    }
}