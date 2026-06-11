namespace AutoStock.Services.Dtos.Pdfs
{
    public class ServicePdfRequestGroupDto
    {
        public int Id { get; set; }

        public string? Title { get; set; }

        public string? Note { get; set; }

        public decimal? EstimatedAmount { get; set; }

        public List<ServicePdfItemDto> Operations { get; set; } = new();
    }
}