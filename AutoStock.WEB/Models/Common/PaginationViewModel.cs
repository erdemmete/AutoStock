namespace AutoStock.WEB.Models.Common
{
    public class PaginationViewModel
    {
        public string Controller { get; set; } = null!;

        public string Action { get; set; } = "Index";

        public Dictionary<string, string?> RouteValues { get; set; } = new();

        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 10;

        public int TotalCount { get; set; }

        public int TotalPages
        {
            get
            {
                if (PageSize <= 0)
                    return 1;

                var totalPages = (int)Math.Ceiling(TotalCount / (double)PageSize);

                return totalPages < 1 ? 1 : totalPages;
            }
        }

        public bool HasPreviousPage => PageNumber > 1;

        public bool HasNextPage => PageNumber < TotalPages;
    }
}