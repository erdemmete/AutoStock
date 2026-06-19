using AutoStock.Repositories.Enums;

namespace AutoStock.Repositories.Entities
{
    public class Invoice
    {
        public int Id { get; set; }

        public int WorkshopId { get; set; }

        public int CustomerId { get; set; }

        public int? ServiceRecordId { get; set; }

        public InvoiceType Type { get; set; } = InvoiceType.Manual;

        public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

        public string InvoiceNumber { get; set; } = null!;

        public DateTime InvoiceDate { get; set; } 
        public string CustomerTitle { get; set; } = null!;

        public string? CustomerTaxOffice { get; set; }

        public string? CustomerTaxNumber { get; set; }

        public string? CustomerTckn { get; set; }

        public string? CustomerAddress { get; set; }

        public string? CustomerEmail { get; set; }

        public string? Plate { get; set; }

        public string? ChassisNumber { get; set; }

        public int? Mileage { get; set; }

        public decimal Subtotal { get; set; }

        public decimal DiscountTotal { get; set; }

        public decimal VatTotal { get; set; }

        public decimal GrandTotal { get; set; }

        public string? Notes { get; set; }

        // İleride e-Arşiv / e-Fatura entegrasyonu için
        public string? ExternalInvoiceId { get; set; }

        public string? ExternalInvoiceNumber { get; set; }

        public string? ExternalUuid { get; set; }
        public string? VehicleBrandName { get; set; }

        public string? VehicleModelName { get; set; }

        public int? VehicleModelYear { get; set; }

        public DateTime CreatedAt { get; set; } 

        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        public Customer Customer { get; set; } = null!;

        public ServiceRecord? ServiceRecord { get; set; }

        public ICollection<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();

    }
}
