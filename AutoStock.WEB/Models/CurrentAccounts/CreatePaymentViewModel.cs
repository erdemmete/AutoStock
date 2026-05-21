namespace AutoStock.Mobile.Models.CurrentAccounts
{
    public class CreatePaymentViewModel
    {
        public int CustomerId { get; set; }

        public decimal Amount { get; set; }

        public DateTime? PaymentDate { get; set; }

        public string? Description { get; set; }
    }
}