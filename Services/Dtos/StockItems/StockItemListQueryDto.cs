namespace AutoStock.Services.Dtos.StockItems
{
    public class StockItemListQueryDto
    {
        public string? Search { get; set; }

        public string? Brand { get; set; }

        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 10;

        public void Normalize()
        {
            if (PageNumber < 1)
                PageNumber = 1;

            if (PageSize < 5)
                PageSize = 5;

            if (PageSize > 50)
                PageSize = 50;

            Search = string.IsNullOrWhiteSpace(Search)
                ? null
                : Search.Trim();

            Brand = string.IsNullOrWhiteSpace(Brand)
                ? null
                : Brand.Trim();

           
        }
    }
}