namespace AutoStock.WEB.Models.StockItems
{
    public class StockItemListQueryViewModel
    {
        public string? Search { get; set; }

        public string? Brand { get; set; }

        public string? Unit { get; set; }

        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 10;

        public void Normalize()
        {
            if (PageNumber < 1)
                PageNumber = 1;

            if (PageSize < 5)
                PageSize = 5;

            if (PageSize > 100)
                PageSize = 100;

            Search = string.IsNullOrWhiteSpace(Search)
                ? null
                : Search.Trim();

            Brand = string.IsNullOrWhiteSpace(Brand)
                ? null
                : Brand.Trim();

            Unit = string.IsNullOrWhiteSpace(Unit)
                ? null
                : Unit.Trim();
        }
    }
}