BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260620200744_AddAccountingInvoiceBatchFlow'
)
BEGIN
    ALTER TABLE [OfficialInvoiceDocuments] ADD [CustomerDeliveredAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260620200744_AddAccountingInvoiceBatchFlow'
)
BEGIN
    ALTER TABLE [OfficialInvoiceDocuments] ADD [CustomerDeliveredByUserId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260620200744_AddAccountingInvoiceBatchFlow'
)
BEGIN
    ALTER TABLE [OfficialInvoiceDocuments] ADD [CustomerDeliveryChannel] nvarchar(40) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260620200744_AddAccountingInvoiceBatchFlow'
)
BEGIN
    ALTER TABLE [OfficialInvoiceDocuments] ADD [ShareToken] nvarchar(128) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260620200744_AddAccountingInvoiceBatchFlow'
)
BEGIN
    EXEC(N'
        UPDATE [OfficialInvoiceDocuments]
        SET [ShareToken] =
            LOWER(
                REPLACE(CONVERT(nvarchar(36), NEWID()), ''-'', '''') +
                REPLACE(CONVERT(nvarchar(36), NEWID()), ''-'', '''')
            )
        WHERE [ShareToken] IS NULL
           OR LTRIM(RTRIM([ShareToken])) = '''';
    ');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260620200744_AddAccountingInvoiceBatchFlow'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[OfficialInvoiceDocuments]') AND [c].[name] = N'ShareToken');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [OfficialInvoiceDocuments] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [OfficialInvoiceDocuments] ALTER COLUMN [ShareToken] nvarchar(128) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260620200744_AddAccountingInvoiceBatchFlow'
)
BEGIN
    ALTER TABLE [AccountingInvoiceRequests] ADD [BatchCompletedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260620200744_AddAccountingInvoiceBatchFlow'
)
BEGIN
    ALTER TABLE [AccountingInvoiceRequests] ADD [BatchToken] nvarchar(128) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260620200744_AddAccountingInvoiceBatchFlow'
)
BEGIN
    ALTER TABLE [AccountingInvoiceRequests] ADD [RequestedByUserId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260620200744_AddAccountingInvoiceBatchFlow'
)
BEGIN
    CREATE UNIQUE INDEX [IX_OfficialInvoiceDocuments_ShareToken] ON [OfficialInvoiceDocuments] ([ShareToken]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260620200744_AddAccountingInvoiceBatchFlow'
)
BEGIN
    CREATE INDEX [IX_AccountingInvoiceRequests_BatchToken] ON [AccountingInvoiceRequests] ([BatchToken]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260620200744_AddAccountingInvoiceBatchFlow'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260620200744_AddAccountingInvoiceBatchFlow', N'10.0.7');
END;

COMMIT;
GO

