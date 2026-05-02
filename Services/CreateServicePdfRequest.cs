namespace AutoStock.Services
{
    public class CreateServicePdfRequest
    {
        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public string? CustomerEmail { get; set; }

        public string? Plate { get; set; }
        public string? Brand { get; set; }
        public string? Model { get; set; }
        public string? ModelYear { get; set; }

        public List<ServicePdfItemDto> Operations { get; set; } = new();
        public string? Note { get; set; }
    }
}
