namespace AutoStock.Services.Dtos.Pdfs
{
    public class ServicePdfItemDto
    {
        public string? Name { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public string? TypeText { get; set; }
        public string? Note { get; set; }
    }
}
