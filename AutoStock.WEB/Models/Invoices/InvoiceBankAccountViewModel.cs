namespace AutoStock.WEB.Models.Invoices;

public class InvoiceBankAccountViewModel
{
    public int Id { get; set; }

    public string BankName { get; set; } = string.Empty;

    public string AccountHolder { get; set; } = string.Empty;

    public string Iban { get; set; } = string.Empty;

    public string CurrencyCode { get; set; } = "TRY";

    public string? BranchName { get; set; }

    public string? AccountNumber { get; set; }

    public string? Description { get; set; }

    public bool IsDefault { get; set; }

    public bool ShowOnInvoices { get; set; }

    public int SortOrder { get; set; }
}
