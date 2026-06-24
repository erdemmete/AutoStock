namespace AutoStock.Services.Dtos.Accounting
{
    public class AccountingEmailRecipientDto
    {
        public int Id { get; set; }
        public string DisplayName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public bool IsDefault { get; set; }
        public DateTime? LastUsedAt { get; set; }
    }

    public class CreateAccountingEmailRecipientDto
    {
        public string? DisplayName { get; set; }
        public string Email { get; set; } = null!;
        public bool IsDefault { get; set; }
    }

    public class SendAccountingInvoiceRequestDto
    {
        public int InvoiceId { get; set; }
        public List<string> RecipientEmails { get; set; } = new();
        public string? NewRecipientEmail { get; set; }
        public string? NewRecipientDisplayName { get; set; }
        public bool SaveNewRecipient { get; set; }
        public string? Message { get; set; }
        public string? PublicBaseUrl { get; set; }
    }

    public class SendAccountingInvoiceRequestResponseDto
    {
        public int SentCount { get; set; }
        public List<string> SentEmails { get; set; } = new();
    }

    public class SendAccountingInvoiceBatchRequestDto
    {
        public List<int> InvoiceIds { get; set; } = new();
        public string RecipientEmail { get; set; } = null!;
        public string? Message { get; set; }
        public string? PublicBaseUrl { get; set; }
    }

    public class SendAccountingInvoiceBatchResponseDto
    {
        public string BatchToken { get; set; } = null!;
        public string RecipientEmail { get; set; } = null!;
        public int RequestedCount { get; set; }
        public int SentCount { get; set; }
        public int SkippedCount { get; set; }
        public string UploadUrl { get; set; } = null!;
        public List<string> Messages { get; set; } = new();
    }

    public class InvoiceAccountingStatusDto
    {
        public int InvoiceId { get; set; }
        public bool HasPendingRequest { get; set; }
        public bool HasOfficialInvoice { get; set; }
        public string StatusText { get; set; } = null!;
        public DateTime? LastSentAt { get; set; }
        public string? LastSentToEmail { get; set; }
        public OfficialInvoiceDocumentDto? LatestOfficialInvoice { get; set; }
    }

    public class AccountingInvoiceRequestPublicDto
    {
        public string Token { get; set; } = null!;
        public int InvoiceId { get; set; }
        public string InvoiceNumber { get; set; } = null!;
        public DateTime InvoiceDate { get; set; }
        public string StatusText { get; set; } = null!;
        public bool CanUpload { get; set; }
        public DateTime ExpiresAt { get; set; }

        public string WorkshopName { get; set; } = null!;
        public string? WorkshopTaxOffice { get; set; }
        public string? WorkshopTaxNumber { get; set; }

        public string CustomerTitle { get; set; } = null!;
        public string? CustomerTaxOffice { get; set; }
        public string? CustomerTaxNumber { get; set; }
        public string? CustomerTckn { get; set; }
        public string? CustomerAddress { get; set; }

        public string? Plate { get; set; }
        public string? VehicleText { get; set; }
        public string? VehicleBrandName { get; set; }
        public string? VehicleModelName { get; set; }
        public string? VehicleVariantName { get; set; }
        public int? VehicleModelYear { get; set; }
        public int? Mileage { get; set; }
        public string? ChassisNumber { get; set; }

        public decimal Subtotal { get; set; }
        public decimal DiscountTotal { get; set; }
        public decimal VatTotal { get; set; }
        public decimal GrandTotal { get; set; }
        public decimal PaidTotal { get; set; }
        public decimal RemainingAmount { get; set; }

        public List<AccountingInvoiceRequestItemDto> Items { get; set; } = new();
        public OfficialInvoiceDocumentDto? OfficialInvoiceDocument { get; set; }
    }

    public class AccountingInvoiceBatchPublicDto
    {
        public string BatchToken { get; set; } = null!;
        public string WorkshopName { get; set; } = null!;
        public string RecipientEmail { get; set; } = null!;
        public string? Message { get; set; }
        public DateTime SentAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool CanUpload { get; set; }
        public int TotalCount { get; set; }
        public int UploadedCount { get; set; }
        public int PendingCount { get; set; }
        public string StatusText { get; set; } = null!;
        public List<AccountingInvoiceBatchItemDto> Items { get; set; } = new();
    }

    public class AccountingInvoiceBatchItemDto
    {
        public int RequestId { get; set; }
        public int InvoiceId { get; set; }
        public string InvoiceNumber { get; set; } = null!;
        public DateTime InvoiceDate { get; set; }
        public string CustomerTitle { get; set; } = null!;
        public string? Plate { get; set; }
        public string? VehicleText { get; set; }
        public string? VehicleBrandName { get; set; }
        public string? VehicleModelName { get; set; }
        public string? VehicleVariantName { get; set; }
        public int? VehicleModelYear { get; set; }
        public int? Mileage { get; set; }
        public string? ChassisNumber { get; set; }
        public decimal GrandTotal { get; set; }
        public string StatusText { get; set; } = null!;
        public bool CanUpload { get; set; }
        public OfficialInvoiceDocumentDto? OfficialInvoiceDocument { get; set; }
    }

    public class CompleteAccountingInvoiceBatchUploadResponseDto
    {
        public int TotalCount { get; set; }
        public int UploadedCount { get; set; }
        public int PendingCount { get; set; }
        public string Message { get; set; } = null!;
    }

    public class MarkOfficialInvoiceDeliveredDto
    {
        public string Channel { get; set; } = null!;
    }

    public class AccountingInvoiceRequestItemDto
    {
        public int ItemType { get; set; }
        public string ItemTypeText { get; set; } = null!;
        public string Description { get; set; } = null!;
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = null!;
        public decimal UnitPrice { get; set; }
        public decimal DiscountRate { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal VatRate { get; set; }
        public decimal VatAmount { get; set; }
        public decimal LineTotal { get; set; }
    }

    public class UploadOfficialInvoiceDto
    {
        public string OfficialInvoiceNumber { get; set; } = null!;
        public DateTime OfficialInvoiceDate { get; set; }
        public string? EttnOrUuid { get; set; }
        public string UploadedByEmail { get; set; } = null!;
        public string? Note { get; set; }

        public string FileName { get; set; } = null!;
        public string? ContentType { get; set; }
        public long FileSizeBytes { get; set; }
        public Stream FileContent { get; set; } = null!;
    }

    public class OfficialInvoiceDocumentDto
    {
        public int Id { get; set; }
        public int InvoiceId { get; set; }
        public string OfficialInvoiceNumber { get; set; } = null!;
        public DateTime OfficialInvoiceDate { get; set; }
        public string? EttnOrUuid { get; set; }
        public string OriginalFileName { get; set; } = null!;
        public long FileSizeBytes { get; set; }
        public DateTime UploadedAt { get; set; }
        public string UploadedByEmail { get; set; } = null!;
        public string? Note { get; set; }
        public string ShareToken { get; set; } = null!;
        public DateTime? CustomerDeliveredAt { get; set; }
        public string? CustomerDeliveryChannel { get; set; }
    }

    public class OfficialInvoiceFileDto
    {
        public string FileName { get; set; } = null!;
        public string ContentType { get; set; } = null!;
        public byte[] Content { get; set; } = Array.Empty<byte>();
    }
}
