namespace AutoStock.WEB.Models.Customers
{
    public class CustomerListItemViewModel
    {
        public int Id { get; set; }

        public string DisplayName { get; set; } = null!;

        public string? PhoneNumber { get; set; }

        public string? Email { get; set; }

        public string TypeText { get; set; } = null!;

        public decimal Balance { get; set; }
    }
}