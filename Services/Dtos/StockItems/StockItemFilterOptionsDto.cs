namespace AutoStock.Services.Dtos.StockItems
{
    public class StockItemFilterOptionsDto
    {
        public List<string> Brands { get; set; } = new();

        public List<string> Units { get; set; } = new();
    }
}