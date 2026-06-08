using AutoStock.Services.Dtos.Common;

namespace AutoStock.WEB.Models.SupportRequests
{
    public class AdminSupportRequestIndexViewModel
    {
        public PagedResult<SupportRequestListItemViewModel> Requests { get; set; } = new()
        {
            Items = new List<SupportRequestListItemViewModel>(),
            PageNumber = 1,
            PageSize = 10,
            TotalCount = 0
        };

        public AdminSupportRequestListQueryViewModel Query { get; set; } = new();
    }
}