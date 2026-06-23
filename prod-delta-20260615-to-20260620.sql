BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260618140713_VehicleQrStatusAndGenerationLimits'
)
BEGIN
    ALTER TABLE [VehicleQrCodes] DROP CONSTRAINT [FK_VehicleQrCodes_Vehicles_VehicleId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260618140713_VehicleQrStatusAndGenerationLimits'
)
BEGIN
    DROP INDEX [IX_VehicleQrCodes_VehicleId] ON [VehicleQrCodes];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260618140713_VehicleQrStatusAndGenerationLimits'
)
BEGIN
    IF EXISTS (
        SELECT [Code]
        FROM [VehicleQrCodes]
        GROUP BY [Code]
        HAVING COUNT(*) > 1
    )
    BEGIN
        THROW 51000, 'VehicleQrCodes migration durduruldu: Aynı Code değerine sahip birden fazla QR kaydı var. Lütfen duplicate QR kodlarını raporlayıp düzeltin.', 1;
    END

    IF EXISTS (
        SELECT 1
        FROM [VehicleQrCodes]
        WHERE LEN([Code]) > 64
    )
    BEGIN
        THROW 51001, 'VehicleQrCodes migration durduruldu: 64 karakterden uzun QR Code değeri var. Lütfen ilgili kayıtları raporlayıp düzeltin.', 1;
    END

    IF EXISTS (
        SELECT 1
        FROM [VehicleQrCodes]
        WHERE [IsAssigned] = 1
          AND ([WorkshopId] IS NULL OR [VehicleId] IS NULL OR [AssignedAt] IS NULL)
    )
    BEGIN
        THROW 51002, 'VehicleQrCodes migration durduruldu: Assigned görünen ama WorkshopId, VehicleId veya AssignedAt alanı eksik QR kaydı var.', 1;
    END

    IF EXISTS (
        SELECT 1
        FROM [VehicleQrCodes]
        WHERE [IsAssigned] = 0
          AND ([VehicleId] IS NOT NULL OR [AssignedAt] IS NOT NULL)
    )
    BEGIN
        THROW 51003, 'VehicleQrCodes migration durduruldu: Atanmamış görünen ama VehicleId veya AssignedAt değeri dolu QR kaydı var.', 1;
    END

    IF EXISTS (
        SELECT [VehicleId]
        FROM [VehicleQrCodes]
        WHERE [IsAssigned] = 1 AND [VehicleId] IS NOT NULL
        GROUP BY [VehicleId]
        HAVING COUNT(*) > 1
    )
    BEGIN
        THROW 51004, 'VehicleQrCodes migration durduruldu: Aynı araçta birden fazla aktif QR kaydı var.', 1;
    END

    IF EXISTS (
        SELECT 1
        FROM [VehicleQrCodes] q
        WHERE q.[WorkshopId] IS NOT NULL
          AND NOT EXISTS (SELECT 1 FROM [Workshops] w WHERE w.[Id] = q.[WorkshopId])
    )
    BEGIN
        THROW 51005, 'VehicleQrCodes migration durduruldu: Var olmayan WorkshopId içeren QR kaydı var.', 1;
    END

    IF EXISTS (
        SELECT 1
        FROM [VehicleQrCodes] q
        WHERE q.[VehicleId] IS NOT NULL
          AND NOT EXISTS (SELECT 1 FROM [Vehicles] v WHERE v.[Id] = q.[VehicleId])
    )
    BEGIN
        THROW 51006, 'VehicleQrCodes migration durduruldu: Var olmayan VehicleId içeren QR kaydı var.', 1;
    END
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260618140713_VehicleQrStatusAndGenerationLimits'
)
BEGIN
    ALTER TABLE [Workshops] ADD [QrGenerationEnabled] bit NOT NULL DEFAULT CAST(1 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260618140713_VehicleQrStatusAndGenerationLimits'
)
BEGIN
    ALTER TABLE [Workshops] ADD [QrGenerationLimit] int NOT NULL DEFAULT 50;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260618140713_VehicleQrStatusAndGenerationLimits'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[VehicleQrCodes]') AND [c].[name] = N'Code');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [VehicleQrCodes] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [VehicleQrCodes] ALTER COLUMN [Code] nvarchar(64) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260618140713_VehicleQrStatusAndGenerationLimits'
)
BEGIN
    ALTER TABLE [VehicleQrCodes] ADD [AssignedByUserId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260618140713_VehicleQrStatusAndGenerationLimits'
)
BEGIN
    ALTER TABLE [VehicleQrCodes] ADD [CreatedByUserId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260618140713_VehicleQrStatusAndGenerationLimits'
)
BEGIN
    ALTER TABLE [VehicleQrCodes] ADD [RetiredAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260618140713_VehicleQrStatusAndGenerationLimits'
)
BEGIN
    ALTER TABLE [VehicleQrCodes] ADD [RetiredByUserId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260618140713_VehicleQrStatusAndGenerationLimits'
)
BEGIN
    ALTER TABLE [VehicleQrCodes] ADD [Status] int NOT NULL DEFAULT 1;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260618140713_VehicleQrStatusAndGenerationLimits'
)
BEGIN
    EXEC(N'
        UPDATE [VehicleQrCodes]
        SET [Status] = CASE
            WHEN [IsAssigned] = 1 THEN 2
            ELSE 1
        END;
    ');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260618140713_VehicleQrStatusAndGenerationLimits'
)
BEGIN
    DECLARE @var1 nvarchar(max);
    SELECT @var1 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[VehicleQrCodes]') AND [c].[name] = N'IsAssigned');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [VehicleQrCodes] DROP CONSTRAINT ' + @var1 + ';');
    ALTER TABLE [VehicleQrCodes] DROP COLUMN [IsAssigned];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260618140713_VehicleQrStatusAndGenerationLimits'
)
BEGIN
    CREATE UNIQUE INDEX [IX_VehicleQrCodes_Code] ON [VehicleQrCodes] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260618140713_VehicleQrStatusAndGenerationLimits'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_VehicleQrCodes_VehicleId] ON [VehicleQrCodes] ([VehicleId]) WHERE [Status] = 2 AND [VehicleId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260618140713_VehicleQrStatusAndGenerationLimits'
)
BEGIN
    CREATE INDEX [IX_VehicleQrCodes_WorkshopId] ON [VehicleQrCodes] ([WorkshopId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260618140713_VehicleQrStatusAndGenerationLimits'
)
BEGIN
    EXEC(N'ALTER TABLE [VehicleQrCodes] ADD CONSTRAINT [CK_VehicleQrCodes_Assigned_State] CHECK (([Status] <> 2 OR ([WorkshopId] IS NOT NULL AND [VehicleId] IS NOT NULL AND [AssignedAt] IS NOT NULL)))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260618140713_VehicleQrStatusAndGenerationLimits'
)
BEGIN
    EXEC(N'ALTER TABLE [VehicleQrCodes] ADD CONSTRAINT [CK_VehicleQrCodes_Available_State] CHECK (([Status] <> 1 OR ([VehicleId] IS NULL AND [AssignedAt] IS NULL AND [RetiredAt] IS NULL)))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260618140713_VehicleQrStatusAndGenerationLimits'
)
BEGIN
    EXEC(N'ALTER TABLE [VehicleQrCodes] ADD CONSTRAINT [CK_VehicleQrCodes_Retired_State] CHECK (([Status] <> 3 OR [RetiredAt] IS NOT NULL))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260618140713_VehicleQrStatusAndGenerationLimits'
)
BEGIN
    EXEC(N'ALTER TABLE [VehicleQrCodes] ADD CONSTRAINT [CK_VehicleQrCodes_Status_Valid] CHECK ([Status] IN (1, 2, 3, 4))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260618140713_VehicleQrStatusAndGenerationLimits'
)
BEGIN
    ALTER TABLE [VehicleQrCodes] ADD CONSTRAINT [FK_VehicleQrCodes_Vehicles_VehicleId] FOREIGN KEY ([VehicleId]) REFERENCES [Vehicles] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260618140713_VehicleQrStatusAndGenerationLimits'
)
BEGIN
    ALTER TABLE [VehicleQrCodes] ADD CONSTRAINT [FK_VehicleQrCodes_Workshops_WorkshopId] FOREIGN KEY ([WorkshopId]) REFERENCES [Workshops] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260618140713_VehicleQrStatusAndGenerationLimits'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260618140713_VehicleQrStatusAndGenerationLimits', N'10.0.7');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260618153000_AddInvoiceCustomerEmailSnapshot'
)
BEGIN
    ALTER TABLE [Invoices] ADD [CustomerEmail] nvarchar(150) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260618153000_AddInvoiceCustomerEmailSnapshot'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260618153000_AddInvoiceCustomerEmailSnapshot', N'10.0.7');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260618170000_AddEntityEditLocksAndRowVersions'
)
BEGIN
    ALTER TABLE [ServiceRecords] ADD [RowVersion] rowversion NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260618170000_AddEntityEditLocksAndRowVersions'
)
BEGIN
    ALTER TABLE [Invoices] ADD [RowVersion] rowversion NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260618170000_AddEntityEditLocksAndRowVersions'
)
BEGIN
    CREATE TABLE [EntityEditLocks] (
        [Id] int NOT NULL IDENTITY,
        [WorkshopId] int NOT NULL,
        [EntityType] nvarchar(32) NOT NULL,
        [EntityId] int NOT NULL,
        [LockedByUserId] int NOT NULL,
        [LockToken] nvarchar(128) NOT NULL,
        [AcquiredAt] datetime2 NOT NULL,
        [LastHeartbeatAt] datetime2 NOT NULL,
        [ExpiresAt] datetime2 NOT NULL,
        CONSTRAINT [PK_EntityEditLocks] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EntityEditLocks_AspNetUsers_LockedByUserId] FOREIGN KEY ([LockedByUserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_EntityEditLocks_Workshops_WorkshopId] FOREIGN KEY ([WorkshopId]) REFERENCES [Workshops] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260618170000_AddEntityEditLocksAndRowVersions'
)
BEGIN
    CREATE INDEX [IX_EntityEditLocks_LockedByUserId_ExpiresAt] ON [EntityEditLocks] ([LockedByUserId], [ExpiresAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260618170000_AddEntityEditLocksAndRowVersions'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_EntityEditLocks_WorkshopId_EntityType_EntityId] ON [EntityEditLocks] ([WorkshopId], [EntityType], [EntityId]) WHERE [WorkshopId] IS NOT NULL AND [EntityType] IS NOT NULL AND [EntityId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260618170000_AddEntityEditLocksAndRowVersions'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260618170000_AddEntityEditLocksAndRowVersions', N'10.0.7');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260619183619_AddWebPushSubscriptions'
)
BEGIN
    CREATE TABLE [WebPushSubscriptions] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [WorkshopId] int NULL,
        [Endpoint] nvarchar(2048) NOT NULL,
        [P256dh] nvarchar(256) NOT NULL,
        [Auth] nvarchar(256) NOT NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [CreatedAt] datetime2 NOT NULL DEFAULT (CAST(SYSUTCDATETIME() AT TIME ZONE 'UTC' AT TIME ZONE 'Turkey Standard Time' AS datetime2)),
        [UpdatedAt] datetime2 NOT NULL DEFAULT (CAST(SYSUTCDATETIME() AT TIME ZONE 'UTC' AT TIME ZONE 'Turkey Standard Time' AS datetime2)),
        [LastSuccessAt] datetime2 NULL,
        [LastFailureAt] datetime2 NULL,
        [UserAgent] nvarchar(500) NULL,
        CONSTRAINT [PK_WebPushSubscriptions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_WebPushSubscriptions_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_WebPushSubscriptions_Workshops_WorkshopId] FOREIGN KEY ([WorkshopId]) REFERENCES [Workshops] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260619183619_AddWebPushSubscriptions'
)
BEGIN
    CREATE UNIQUE INDEX [IX_WebPushSubscriptions_Endpoint] ON [WebPushSubscriptions] ([Endpoint]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260619183619_AddWebPushSubscriptions'
)
BEGIN
    CREATE INDEX [IX_WebPushSubscriptions_UserId] ON [WebPushSubscriptions] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260619183619_AddWebPushSubscriptions'
)
BEGIN
    CREATE INDEX [IX_WebPushSubscriptions_UserId_IsActive] ON [WebPushSubscriptions] ([UserId], [IsActive]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260619183619_AddWebPushSubscriptions'
)
BEGIN
    CREATE INDEX [IX_WebPushSubscriptions_WorkshopId_IsActive] ON [WebPushSubscriptions] ([WorkshopId], [IsActive]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260619183619_AddWebPushSubscriptions'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260619183619_AddWebPushSubscriptions', N'10.0.7');
END;

COMMIT;
GO

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
    DECLARE @var2 nvarchar(max);
    SELECT @var2 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[OfficialInvoiceDocuments]') AND [c].[name] = N'ShareToken');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [OfficialInvoiceDocuments] DROP CONSTRAINT ' + @var2 + ';');
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

