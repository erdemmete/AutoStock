namespace AutoStock.Repositories.Entities
{
    public class OfficialInvoiceDocument
    {
        public int Id { get; set; }

        public int WorkshopId { get; set; }

        public int InvoiceId { get; set; }

        public int? AccountingInvoiceRequestId { get; set; }

        public string OfficialInvoiceNumber { get; set; } = null!;

        public DateTime OfficialInvoiceDate { get; set; }

        public string? EttnOrUuid { get; set; }

        public string OriginalFileName { get; set; } = null!;

        public string StoredFileName { get; set; } = null!;

        public string RelativePath { get; set; } = null!;

        public string ContentType { get; set; } = null!;

        public long FileSizeBytes { get; set; }

        public DateTime UploadedAt { get; set; }

        public string UploadedByEmail { get; set; } = null!;

        public string? Note { get; set; }

        public string ShareToken { get; set; } = null!;

        public DateTime? CustomerDeliveredAt { get; set; }

        public int? CustomerDeliveredByUserId { get; set; }

        public string? CustomerDeliveryChannel { get; set; }

        public Workshop Workshop { get; set; } = null!;

        public Invoice Invoice { get; set; } = null!;

        public AccountingInvoiceRequest? AccountingInvoiceRequest { get; set; }
    }
}
