namespace AutoStock.WEB.Models.Admin.Workshops
{
    public class AdminWorkshopBankAccountViewModel
    {
        public int Id { get; set; }

        public int WorkshopId { get; set; }

        public string BankName { get; set; } = null!;

        public string AccountHolder { get; set; } = null!;

        public string Iban { get; set; } = null!;

        public string CurrencyCode { get; set; } = "TRY";

        public string? BranchName { get; set; }

        public string? AccountNumber { get; set; }

        public string? Description { get; set; }

        public bool IsDefault { get; set; }

        public bool ShowOnInvoices { get; set; } = true;

        public bool ShowOnServiceForms { get; set; }

        public bool IsActive { get; set; } = true;

        public int SortOrder { get; set; }
    }

    public class CreateAdminWorkshopBankAccountViewModel
    {
        public int WorkshopId { get; set; }

        public string BankName { get; set; } = null!;

        public string AccountHolder { get; set; } = null!;

        public string Iban { get; set; } = null!;

        public string CurrencyCode { get; set; } = "TRY";

        public string? BranchName { get; set; }

        public string? AccountNumber { get; set; }

        public string? Description { get; set; }

        public bool IsDefault { get; set; }

        public bool ShowOnInvoices { get; set; } = true;

        public bool ShowOnServiceForms { get; set; }

        public int SortOrder { get; set; }
    }

    public class UpdateAdminWorkshopBankAccountViewModel
    {
        public int WorkshopId { get; set; }

        public int BankAccountId { get; set; }

        public string BankName { get; set; } = null!;

        public string AccountHolder { get; set; } = null!;

        public string Iban { get; set; } = null!;

        public string CurrencyCode { get; set; } = "TRY";

        public string? BranchName { get; set; }

        public string? AccountNumber { get; set; }

        public string? Description { get; set; }

        public bool IsDefault { get; set; }

        public bool ShowOnInvoices { get; set; } = true;

        public bool ShowOnServiceForms { get; set; }

        public bool IsActive { get; set; } = true;

        public int SortOrder { get; set; }
    }
}
