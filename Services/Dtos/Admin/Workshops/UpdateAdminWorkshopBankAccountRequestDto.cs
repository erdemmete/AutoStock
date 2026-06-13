namespace AutoStock.Services.Dtos.Admin.Workshops
{
    public class UpdateAdminWorkshopBankAccountRequestDto
    {
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
