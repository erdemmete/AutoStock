namespace AutoStock.Repositories.Entities
{
    public class WorkshopBankAccount
    {
        public int Id { get; set; }

        public int WorkshopId { get; set; }

        public Workshop Workshop { get; set; } = null!;

        public string BankName { get; set; } = null!;

        public string AccountHolder { get; set; } = null!;

        public string Iban { get; set; } = null!;

        public string CurrencyCode { get; set; } = "TRY";

        public string? BranchName { get; set; }

        public string? AccountNumber { get; set; }

        public string? Description { get; set; }

        public bool IsDefault { get; set; }

        public bool ShowOnInvoices { get; set; } = true;

        public bool ShowOnServiceForms { get; set; } = false;

        public bool IsActive { get; set; } = true;

        public int SortOrder { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
}
