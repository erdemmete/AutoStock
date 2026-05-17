using AutoStock.Repositories.Enums;

namespace AutoStock.Repositories.Entities
{
    public class InvoiceItem
    {
        public int Id { get; set; }

        public int InvoiceId { get; set; }

        public InvoiceItemType ItemType { get; set; } = InvoiceItemType.Other;

        public string Description { get; set; } = null!;

        public decimal Quantity { get; set; } = 1;

        public string Unit { get; set; } = "Adet";

        public decimal UnitPrice { get; set; }

        public decimal DiscountRate { get; set; }

        public decimal DiscountAmount { get; set; }

        public decimal VatRate { get; set; } = 20;

        public decimal VatAmount { get; set; }

        public decimal LineTotal { get; set; }

        public Invoice Invoice { get; set; } = null!;
    }
}