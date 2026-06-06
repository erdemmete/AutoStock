using AutoStock.WEB.Models.Common;

namespace AutoStock.WEB.Models.Admin.Workshops
{
    public class AdminWorkshopIndexViewModel
    {
        public AdminWorkshopListQueryViewModel Query { get; set; } = new();

        public PagedResultViewModel<AdminWorkshopListItemViewModel> Workshops { get; set; } = new();
    }
}