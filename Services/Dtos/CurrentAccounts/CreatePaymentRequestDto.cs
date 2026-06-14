namespace AutoStock.Services.Dtos.CurrentAccounts
{
    public class CreatePaymentRequestDto
    {
        public int CustomerId { get; set; }

        public int? InvoiceId { get; set; }

        public decimal Amount { get; set; }

        public DateTime? PaymentDate { get; set; }

        public string? PaymentMethod { get; set; }

        public string? Description { get; set; }
    }
}
