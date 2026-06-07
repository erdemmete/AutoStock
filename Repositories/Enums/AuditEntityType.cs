namespace AutoStock.Repositories.Enums
{
    public enum AuditEntityType
    {
        Customer = 1,

        StockItem = 10,
        StockMovement = 11,

        ServiceRecord = 20,
        ServiceOperation = 21,

        Invoice = 30,
        CurrentAccountTransaction = 31,

        Workshop = 40,
        WorkshopUser = 41,
        WorkshopSubscription = 42
    }
}