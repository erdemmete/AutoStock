namespace AutoStock.Repositories.Enums
{
    public enum NotificationType
    {
        General = 0,

        SupportRequestCreated = 10,
        SupportRequestAnswered = 11,
        SupportRequestStatusChanged = 12,

        InvoiceDocumentUploaded = 30,
        InvoiceDocumentReuploaded = 31,

        System = 100
    }
}
