using AutoStock.Repositories.Enums;

namespace AutoStock.Repositories.Entities
{
    public class AccountingInvoiceRequest
    {
        public int Id { get; set; }

        public int WorkshopId { get; set; }

        public int InvoiceId { get; set; }

        public string Token { get; set; } = null!;

        public string? BatchToken { get; set; }

        public string AccountantEmail { get; set; } = null!;

        public string? Message { get; set; }

        public int? RequestedByUserId { get; set; }

        public AccountingInvoiceRequestStatus Status { get; set; } = AccountingInvoiceRequestStatus.Pending;

        public DateTime SentAt { get; set; }

        public DateTime ExpiresAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        public DateTime? BatchCompletedAt { get; set; }

        public DateTime CreatedAt { get; set; }

        public Workshop Workshop { get; set; } = null!;

        public Invoice Invoice { get; set; } = null!;

        public ICollection<OfficialInvoiceDocument> OfficialInvoiceDocuments { get; set; } = new List<OfficialInvoiceDocument>();
    }
}
