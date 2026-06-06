using AutoStock.WEB.Models.Common;

namespace AutoStock.Web.Models.ServiceRecords
{
    public class ServiceRecordIndexViewModel
    {
        public ServiceRecordListQueryViewModel Query { get; set; } = new();

        public PagedResultViewModel<ServiceRecordListItemViewModel> ServiceRecords { get; set; } = new();
    }
}