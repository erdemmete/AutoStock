namespace AutoStock.Services.Dtos.Pdfs
{
    public class CreateQuickOfferPdfRequest
    {
        public string? WorkshopName { get; set; }
        public string? ServiceAdvisorName { get; set; }

        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public string? CustomerEmail { get; set; }

        public string? Plate { get; set; }
        public string? Brand { get; set; }
        public string? Model { get; set; }
        public string? ModelYear { get; set; }
        public string? Mileage { get; set; }
        public string? ChassisNumber { get; set; }

        public string? Note { get; set; }

        public List<QuickOfferPdfItemDto> RequestItems { get; set; } = new();
    }

    public class QuickOfferPdfItemDto
    {
        public string? Title { get; set; }
        public string? Note { get; set; }
        public decimal? EstimatedAmount { get; set; }
    }
}