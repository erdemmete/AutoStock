namespace AutoStock.WEB.Models.Common
{
    public class PagedResultViewModel<T>
    {
        public List<T> Items { get; set; } = new();

        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 10;

        public int TotalCount { get; set; }

        public int TotalPages
        {
            get
            {
                if (PageSize <= 0)
                    return 0;

                return (int)Math.Ceiling(TotalCount / (double)PageSize);
            }
        }

        public bool HasPreviousPage => PageNumber > 1;

        public bool HasNextPage => PageNumber < TotalPages;

        public int FirstItemNumber => TotalCount == 0
            ? 0
            : ((PageNumber - 1) * PageSize) + 1;

        public int LastItemNumber => Math.Min(PageNumber * PageSize, TotalCount);
    }
}