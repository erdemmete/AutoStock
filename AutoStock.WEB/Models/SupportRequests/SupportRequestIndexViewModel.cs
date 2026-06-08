using AutoStock.Services.Dtos.Common;

namespace AutoStock.WEB.Models.SupportRequests
{
    public class SupportRequestIndexViewModel
    {
        public PagedResult<SupportRequestListItemViewModel> Requests { get; set; } = new()
        {
            Items = new List<SupportRequestListItemViewModel>(),
            PageNumber = 1,
            PageSize = 10,
            TotalCount = 0
        };

        public SupportRequestListQueryViewModel Query { get; set; } = new();

        public bool IsOwner { get; set; }
    }
}