namespace AutoStock.Services.Dtos.CurrentAccounts
{
    public class CurrentAccountOpenInvoiceDto
    {
        public int InvoiceId { get; set; }

        public int? ServiceRecordId { get; set; }

        public string InvoiceNumber { get; set; } = null!;

        public DateTime InvoiceDate { get; set; }

        public string? Plate { get; set; }

        public decimal GrandTotal { get; set; }

        public decimal PaidTotal { get; set; }

        public decimal RemainingAmount { get; set; }
    }
}
