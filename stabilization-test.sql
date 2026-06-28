BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260624220358_PreventDuplicateActiveInvoices'
)
BEGIN
    ALTER TABLE [ServiceRecords] ADD [ClientRequestId] nvarchar(64) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260624220358_PreventDuplicateActiveInvoices'
)
BEGIN
    EXEC(N'
    IF COL_LENGTH(''dbo.ServiceRecords'', ''ClientRequestId'') IS NOT NULL
       AND EXISTS (
            SELECT 1
            FROM [ServiceRecords]
            WHERE [ClientRequestId] IS NOT NULL
            GROUP BY [WorkshopId], [ClientRequestId]
            HAVING COUNT(*) > 1
       )
    BEGIN
        THROW 51001, ''ClientRequestId unique index oluşturulamadı. Aynı workshop içinde aynı ClientRequestId değerine sahip birden fazla servis kaydı bulundu.'', 1;
    END
    ')
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260624220358_PreventDuplicateActiveInvoices'
)
BEGIN
    EXEC(N'
    IF EXISTS (
        SELECT 1
        FROM [Invoices]
        WHERE [ServiceRecordId] IS NOT NULL
          AND [Status] IN (1, 2)
        GROUP BY [ServiceRecordId]
        HAVING COUNT(*) > 1
    )
    BEGIN
        THROW 51002, ''Aktif fatura unique index oluşturulamadı. Aynı servis kaydına bağlı birden fazla Draft/Issued fatura bulundu.'', 1;
    END
    ')
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260624220358_PreventDuplicateActiveInvoices'
)
BEGIN
    DROP INDEX [IX_Invoices_ServiceRecordId] ON [Invoices];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260624220358_PreventDuplicateActiveInvoices'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_ServiceRecords_WorkshopId_ClientRequestId] ON [ServiceRecords] ([WorkshopId], [ClientRequestId]) WHERE [ClientRequestId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260624220358_PreventDuplicateActiveInvoices'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_Invoices_Active_ServiceRecordId] ON [Invoices] ([ServiceRecordId]) WHERE [ServiceRecordId] IS NOT NULL AND [Status] IN (1, 2)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260624220358_PreventDuplicateActiveInvoices'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260624220358_PreventDuplicateActiveInvoices', N'10.0.7');
END;

COMMIT;
GO

