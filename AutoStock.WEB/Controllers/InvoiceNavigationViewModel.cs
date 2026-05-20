namespace AutoStock.WEB.Models.Invoices;

public class InvoiceNavigationViewModel
{
    public int InvoiceId { get; set; }
    public int Status { get; set; }
    public string InvoiceNumber { get; set; } = null!;
}