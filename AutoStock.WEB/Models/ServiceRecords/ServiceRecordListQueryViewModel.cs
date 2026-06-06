namespace AutoStock.Web.Models.ServiceRecords
{
    public class ServiceRecordListQueryViewModel
    {
        public string? Search { get; set; }

        public string StatusFilter { get; set; } = "active";

        public DateTime? CreatedFrom { get; set; }

        public DateTime? CreatedTo { get; set; }

        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 10;
    }
}