namespace AutoStock.WEB.Models.StockItems
{
    public class StockItemFilterOptionsViewModel
    {
        public List<string> Brands { get; set; } = new();

        public List<string> Units { get; set; } = new();
    }
}