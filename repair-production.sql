IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260502205842_InitAuth'
)
BEGIN
    CREATE TABLE [Customers] (
        [Id] int NOT NULL IDENTITY,
        [FullName] nvarchar(100) NOT NULL,
        [PhoneNumber] nvarchar(20) NOT NULL,
        [Email] nvarchar(100) NULL,
        [Address] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        CONSTRAINT [PK_Customers] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260502205842_InitAuth'
)
BEGIN
    CREATE TABLE [Employee] (
        [Id] int NOT NULL IDENTITY,
        [FullName] nvarchar(100) NOT NULL,
        [PhoneNumber] nvarchar(20) NOT NULL,
        [Role] nvarchar(50) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        CONSTRAINT [PK_Employee] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260502205842_InitAuth'
)
BEGIN
    CREATE TABLE [Vehicle] (
        [Id] int NOT NULL IDENTITY,
        [PlateNumber] nvarchar(20) NOT NULL,
        [Brand] nvarchar(50) NOT NULL,
        [Model] nvarchar(50) NOT NULL,
        [Year] int NULL,
        [VinNumber] nvarchar(max) NULL,
        [EngineNumber] nvarchar(max) NULL,
        [Kilometer] int NULL,
        [CustomerId] int NOT NULL,
        CONSTRAINT [PK_Vehicle] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Vehicle_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260502205842_InitAuth'
)
BEGIN
    CREATE TABLE [ServiceRecord] (
        [Id] int NOT NULL IDENTITY,
        [VehicleId] int NOT NULL,
        [EmployeeId] int NULL,
        [ServiceDate] datetime2 NOT NULL,
        [Complaint] nvarchar(500) NOT NULL,
        [Diagnosis] nvarchar(500) NULL,
        [Notes] nvarchar(500) NULL,
        [Status] int NOT NULL,
        [LaborCost] decimal(18,2) NULL,
        [TotalCost] decimal(18,2) NULL,
        CONSTRAINT [PK_ServiceRecord] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ServiceRecord_Employee_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employee] ([Id]),
        CONSTRAINT [FK_ServiceRecord_Vehicle_VehicleId] FOREIGN KEY ([VehicleId]) REFERENCES [Vehicle] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260502205842_InitAuth'
)
BEGIN
    CREATE TABLE [RepairRecord] (
        [Id] int NOT NULL IDENTITY,
        [ServiceRecordId] int NOT NULL,
        [RepairDescription] nvarchar(300) NOT NULL,
        [UsedParts] nvarchar(max) NULL,
        [PartCost] decimal(18,2) NULL,
        [LaborCost] decimal(18,2) NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_RepairRecord] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RepairRecord_ServiceRecord_ServiceRecordId] FOREIGN KEY ([ServiceRecordId]) REFERENCES [ServiceRecord] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260502205842_InitAuth'
)
BEGIN
    CREATE INDEX [IX_RepairRecord_ServiceRecordId] ON [RepairRecord] ([ServiceRecordId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260502205842_InitAuth'
)
BEGIN
    CREATE INDEX [IX_ServiceRecord_EmployeeId] ON [ServiceRecord] ([EmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260502205842_InitAuth'
)
BEGIN
    CREATE INDEX [IX_ServiceRecord_VehicleId] ON [ServiceRecord] ([VehicleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260502205842_InitAuth'
)
BEGIN
    CREATE INDEX [IX_Vehicle_CustomerId] ON [Vehicle] ([CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260502205842_InitAuth'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260502205842_InitAuth', N'10.0.7');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260502213112_AddIdentityAndWorkshop'
)
BEGIN
    ALTER TABLE [RepairRecord] DROP CONSTRAINT [FK_RepairRecord_ServiceRecord_ServiceRecordId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260502213112_AddIdentityAndWorkshop'
)
BEGIN
    ALTER TABLE [ServiceRecord] DROP CONSTRAINT [FK_ServiceRecord_Employee_EmployeeId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260502213112_AddIdentityAndWorkshop'
)
BEGIN
    ALTER TABLE [ServiceRecord] DROP CONSTRAINT [FK_ServiceRecord_Vehicle_VehicleId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260502213112_AddIdentityAndWorkshop'
)
BEGIN
    ALTER TABLE [Vehicle] DROP CONSTRAINT [FK_Vehicle_Customers_CustomerId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260502213112_AddIdentityAndWorkshop'
)
BEGIN
    ALTER TABLE [Vehicle] DROP CONSTRAINT [PK_Vehicle];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260502213112_AddIdentityAndWorkshop'
)
BEGIN
    ALTER TABLE [ServiceRecord] DROP CONSTRAINT [PK_ServiceRecord];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260502213112_AddIdentityAndWorkshop'
)
BEGIN
    ALTER TABLE [RepairRecord] DROP CONSTRAINT [PK_RepairRecord];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260502213112_AddIdentityAndWorkshop'
)
BEGIN
    ALTER TABLE [Employee] DROP CONSTRAINT [PK_Employee];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260502213112_AddIdentityAndWorkshop'
)
BEGIN
    EXEC sp_rename N'[Vehicle]', N'Vehicles', 'OBJECT';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260502213112_AddIdentityAndWorkshop'
)
BEGIN
    EXEC sp_rename N'[ServiceRecord]', N'ServiceRecords', 'OBJECT';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260502213112_AddIdentityAndWorkshop'
)
BEGIN
    EXEC sp_rename N'[RepairRecord]', N'RepairRecords', 'OBJECT';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260502213112_AddIdentityAndWorkshop'
)
BEGIN
    EXEC sp_rename N'[Employee]', N'Employees', 'OBJECT';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260502213112_AddIdentityAndWorkshop'
)
BEGIN
    EXEC sp_rename N'[Vehicles].[IX_Vehicle_CustomerId]', N'IX_Vehicles_CustomerId', 'INDEX';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260502213112_AddIdentityAndWorkshop'
)
BEGIN
    EXEC sp_rename N'[ServiceRecords].[IX_ServiceRecord_VehicleId]', N'IX_ServiceRecords_VehicleId', 'INDEX';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260502213112_AddIdentityAndWorkshop'
)
BEGIN
    EXEC sp_rename N'[ServiceRecords].[IX_ServiceRecord_EmployeeId]', N'IX_ServiceRecords_EmployeeId', 'INDEX';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260502213112_AddIdentityAndWorkshop'
)
BEGIN
    EXEC sp_rename N'[RepairRecords].[IX_RepairRecord_ServiceRecordId]', N'IX_RepairRecords_ServiceRecordId', 'INDEX';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260502213112_AddIdentityAndWorkshop'
)
BEGIN
    ALTER TABLE [Vehicles] ADD CONSTRAINT [PK_Vehicles] PRIMARY KEY ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260502213112_AddIdentityAndWorkshop'
)
BEGIN
    ALTER TABLE [ServiceRecords] ADD CONSTRAINT [PK_ServiceRecords] PRIMARY KEY ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260502213112_AddIdentityAndWorkshop'
)
BEGIN
    ALTER TABLE [RepairRecords] ADD CONSTRAINT [PK_RepairRecords] PRIMARY KEY ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260502213112_AddIdentityAndWorkshop'
)
BEGIN
    ALTER TABLE [Employees] ADD CONSTRAINT [PK_Employees] PRIMARY KEY ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260502213112_AddIdentityAndWorkshop'
)
BEGIN
    CREATE TABLE [AspNetRoles] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260502213112_AddIdentityAndWorkshop'
)
BEGIN
    CREATE TABLE [AspNetUsers] (
        [Id] int NOT NULL IDENTITY,
        [FullName] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UserName] nvarchar(256) NULL,
        [NormalizedUserName] nvarchar(256) NULL,
        [Email] nvarchar(256) NULL,
        [NormalizedEmail] nvarchar(256) NULL,
        [EmailConfirmed] bit NOT NULL,
        [PasswordHash] nvarchar(max) NULL,
        [SecurityStamp] nvarchar(max) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        [PhoneNumber] nvarchar(max) NULL,
        [PhoneNumberConfirmed] bit NOT NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [LockoutEnd] datetimeoffset NULL,
        [LockoutEnabled] bit NOT NULL,
        [AccessFailedCount] int NOT NULL,
        CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260502213112_AddIdentityAndWorkshop'
)
BEGIN
    CREATE TABLE [Workshops] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(150) NOT NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        CONSTRAINT [PK_Workshops] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260502213112_AddIdentityAndWorkshop'
)
BEGIN
    CREATE TABLE [AspNetRoleClaims] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] int NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260502213112_AddIdentityAndWorkshop'
)
BEGIN
    CREATE TABLE [AspNetUserClaims] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260502213112_AddIdentityAndWorkshop'
)
BEGIN
    CREATE TABLE [AspNetUserLogins] (
        [LoginProvider] nvarchar(450) NOT NULL,
        [ProviderKey] nvarchar(450) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] int NOT NULL,
        CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260502213112_AddIdentityAndWorkshop'
)
BEGIN
    CREATE TABLE [AspNetUserRoles] (
        [UserId] int NOT NULL,
        [RoleId] int NOT NULL,
        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260502213112_AddIdentityAndWorkshop'
)
BEGIN
    CREATE TABLE [AspNetUserTokens] (
        [UserId] int NOT NULL,
        [LoginProvider] nvarchar(450) NOT NULL,
        [Name] nvarchar(450) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260502213112_AddIdentityAndWorkshop'
)
BEGIN
    CREATE TABLE [WorkshopUsers] (
        [Id] int NOT NULL IDENTITY,
        [WorkshopId] int NOT NULL,
        [UserId] int NOT NULL,
        [Role] nvarchar(50) NOT NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        CONSTRAINT [PK_WorkshopUsers] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_WorkshopUsers_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_WorkshopUsers_Workshops_WorkshopId] FOREIGN KEY ([WorkshopId]) REFERENCES [Workshops] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260502213112_AddIdentityAndWorkshop'
)
BEGIN
    CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260502213112_AddIdentityAndWorkshop'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260502213112_AddIdentityAndWorkshop'
)
BEGIN
    CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260502213112_AddIdentityAndWorkshop'
)
BEGIN
    CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260502213112_AddIdentityAndWorkshop'
)
BEGIN
    CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260502213112_AddIdentityAndWorkshop'
)
BEGIN
    CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260502213112_AddIdentityAndWorkshop'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260502213112_AddIdentityAndWorkshop'
)
BEGIN
    CREATE INDEX [IX_WorkshopUsers_UserId] ON [WorkshopUsers] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260502213112_AddIdentityAndWorkshop'
)
BEGIN
    CREATE UNIQUE INDEX [IX_WorkshopUsers_WorkshopId_UserId] ON [WorkshopUsers] ([WorkshopId], [UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260502213112_AddIdentityAndWorkshop'
)
BEGIN
    ALTER TABLE [RepairRecords] ADD CONSTRAINT [FK_RepairRecords_ServiceRecords_ServiceRecordId] FOREIGN KEY ([ServiceRecordId]) REFERENCES [ServiceRecords] ([Id]) ON DELETE CASCADE;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260502213112_AddIdentityAndWorkshop'
)
BEGIN
    ALTER TABLE [ServiceRecords] ADD CONSTRAINT [FK_ServiceRecords_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260502213112_AddIdentityAndWorkshop'
)
BEGIN
    ALTER TABLE [ServiceRecords] ADD CONSTRAINT [FK_ServiceRecords_Vehicles_VehicleId] FOREIGN KEY ([VehicleId]) REFERENCES [Vehicles] ([Id]) ON DELETE CASCADE;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260502213112_AddIdentityAndWorkshop'
)
BEGIN
    ALTER TABLE [Vehicles] ADD CONSTRAINT [FK_Vehicles_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]) ON DELETE CASCADE;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260502213112_AddIdentityAndWorkshop'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260502213112_AddIdentityAndWorkshop', N'10.0.7');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503175025_AddIsActiveToUser'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [IsActive] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503175025_AddIsActiveToUser'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260503175025_AddIsActiveToUser', N'10.0.7');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260504095747_AddDecimalPrecision'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260504095747_AddDecimalPrecision', N'10.0.7');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507112119_InitialServiceRecordStructure'
)
BEGIN
    ALTER TABLE [ServiceRecords] DROP CONSTRAINT [FK_ServiceRecords_Vehicles_VehicleId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507112119_InitialServiceRecordStructure'
)
BEGIN
    ALTER TABLE [Vehicles] DROP CONSTRAINT [FK_Vehicles_Customers_CustomerId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507112119_InitialServiceRecordStructure'
)
BEGIN
    DROP TABLE [RepairRecords];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507112119_InitialServiceRecordStructure'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Vehicles]') AND [c].[name] = N'Brand');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [Vehicles] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [Vehicles] DROP COLUMN [Brand];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507112119_InitialServiceRecordStructure'
)
BEGIN
    DECLARE @var1 nvarchar(max);
    SELECT @var1 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Vehicles]') AND [c].[name] = N'EngineNumber');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Vehicles] DROP CONSTRAINT ' + @var1 + ';');
    ALTER TABLE [Vehicles] DROP COLUMN [EngineNumber];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507112119_InitialServiceRecordStructure'
)
BEGIN
    DECLARE @var2 nvarchar(max);
    SELECT @var2 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Vehicles]') AND [c].[name] = N'Model');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [Vehicles] DROP CONSTRAINT ' + @var2 + ';');
    ALTER TABLE [Vehicles] DROP COLUMN [Model];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507112119_InitialServiceRecordStructure'
)
BEGIN
    DECLARE @var3 nvarchar(max);
    SELECT @var3 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ServiceRecords]') AND [c].[name] = N'Complaint');
    IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [ServiceRecords] DROP CONSTRAINT ' + @var3 + ';');
    ALTER TABLE [ServiceRecords] DROP COLUMN [Complaint];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507112119_InitialServiceRecordStructure'
)
BEGIN
    DECLARE @var4 nvarchar(max);
    SELECT @var4 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ServiceRecords]') AND [c].[name] = N'Diagnosis');
    IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [ServiceRecords] DROP CONSTRAINT ' + @var4 + ';');
    ALTER TABLE [ServiceRecords] DROP COLUMN [Diagnosis];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507112119_InitialServiceRecordStructure'
)
BEGIN
    DECLARE @var5 nvarchar(max);
    SELECT @var5 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ServiceRecords]') AND [c].[name] = N'LaborCost');
    IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [ServiceRecords] DROP CONSTRAINT ' + @var5 + ';');
    ALTER TABLE [ServiceRecords] DROP COLUMN [LaborCost];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507112119_InitialServiceRecordStructure'
)
BEGIN
    DECLARE @var6 nvarchar(max);
    SELECT @var6 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ServiceRecords]') AND [c].[name] = N'Notes');
    IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [ServiceRecords] DROP CONSTRAINT ' + @var6 + ';');
    ALTER TABLE [ServiceRecords] DROP COLUMN [Notes];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507112119_InitialServiceRecordStructure'
)
BEGIN
    DECLARE @var7 nvarchar(max);
    SELECT @var7 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ServiceRecords]') AND [c].[name] = N'TotalCost');
    IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [ServiceRecords] DROP CONSTRAINT ' + @var7 + ';');
    ALTER TABLE [ServiceRecords] DROP COLUMN [TotalCost];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507112119_InitialServiceRecordStructure'
)
BEGIN
    EXEC sp_rename N'[Vehicles].[Year]', N'VehicleModelId', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507112119_InitialServiceRecordStructure'
)
BEGIN
    EXEC sp_rename N'[Vehicles].[PlateNumber]', N'Plate', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507112119_InitialServiceRecordStructure'
)
BEGIN
    EXEC sp_rename N'[Vehicles].[Kilometer]', N'VehicleBrandId', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507112119_InitialServiceRecordStructure'
)
BEGIN
    EXEC sp_rename N'[ServiceRecords].[ServiceDate]', N'CreatedAt', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507112119_InitialServiceRecordStructure'
)
BEGIN
    DECLARE @var8 nvarchar(max);
    SELECT @var8 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Vehicles]') AND [c].[name] = N'VinNumber');
    IF @var8 IS NOT NULL EXEC(N'ALTER TABLE [Vehicles] DROP CONSTRAINT ' + @var8 + ';');
    ALTER TABLE [Vehicles] ALTER COLUMN [VinNumber] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507112119_InitialServiceRecordStructure'
)
BEGIN
    ALTER TABLE [Vehicles] ADD [CreatedAt] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507112119_InitialServiceRecordStructure'
)
BEGIN
    ALTER TABLE [Vehicles] ADD [IsActive] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507112119_InitialServiceRecordStructure'
)
BEGIN
    ALTER TABLE [Vehicles] ADD [Mileage] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507112119_InitialServiceRecordStructure'
)
BEGIN
    ALTER TABLE [Vehicles] ADD [ModelYear] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507112119_InitialServiceRecordStructure'
)
BEGIN
    ALTER TABLE [Vehicles] ADD [WorkshopId] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507112119_InitialServiceRecordStructure'
)
BEGIN
    ALTER TABLE [ServiceRecords] ADD [CompletedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507112119_InitialServiceRecordStructure'
)
BEGIN
    ALTER TABLE [ServiceRecords] ADD [CustomerComplaint] nvarchar(1000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507112119_InitialServiceRecordStructure'
)
BEGIN
    ALTER TABLE [ServiceRecords] ADD [CustomerId] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507112119_InitialServiceRecordStructure'
)
BEGIN
    ALTER TABLE [ServiceRecords] ADD [CustomerNameSnapshot] nvarchar(150) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507112119_InitialServiceRecordStructure'
)
BEGIN
    ALTER TABLE [ServiceRecords] ADD [CustomerPhoneSnapshot] nvarchar(20) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507112119_InitialServiceRecordStructure'
)
BEGIN
    ALTER TABLE [ServiceRecords] ADD [MileageSnapshot] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507112119_InitialServiceRecordStructure'
)
BEGIN
    ALTER TABLE [ServiceRecords] ADD [RepairNote] nvarchar(2000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507112119_InitialServiceRecordStructure'
)
BEGIN
    ALTER TABLE [ServiceRecords] ADD [ServiceReceptionNote] nvarchar(1000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507112119_InitialServiceRecordStructure'
)
BEGIN
    ALTER TABLE [ServiceRecords] ADD [ShowPricesOnPdf] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507112119_InitialServiceRecordStructure'
)
BEGIN
    ALTER TABLE [ServiceRecords] ADD [TotalAmount] decimal(18,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507112119_InitialServiceRecordStructure'
)
BEGIN
    ALTER TABLE [ServiceRecords] ADD [VehicleBrandNameSnapshot] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507112119_InitialServiceRecordStructure'
)
BEGIN
    ALTER TABLE [ServiceRecords] ADD [VehicleModelNameSnapshot] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507112119_InitialServiceRecordStructure'
)
BEGIN
    ALTER TABLE [ServiceRecords] ADD [VehiclePlateSnapshot] nvarchar(20) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507112119_InitialServiceRecordStructure'
)
BEGIN
    ALTER TABLE [ServiceRecords] ADD [WorkshopId] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507112119_InitialServiceRecordStructure'
)
BEGIN
    ALTER TABLE [Customers] ADD [AuthorizedPersonName] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507112119_InitialServiceRecordStructure'
)
BEGIN
    ALTER TABLE [Customers] ADD [CompanyName] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507112119_InitialServiceRecordStructure'
)
BEGIN
    ALTER TABLE [Customers] ADD [IsActive] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507112119_InitialServiceRecordStructure'
)
BEGIN
    ALTER TABLE [Customers] ADD [TaxNumber] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507112119_InitialServiceRecordStructure'
)
BEGIN
    ALTER TABLE [Customers] ADD [TaxOffice] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507112119_InitialServiceRecordStructure'
)
BEGIN
    ALTER TABLE [Customers] ADD [Type] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507112119_InitialServiceRecordStructure'
)
BEGIN
    ALTER TABLE [Customers] ADD [WorkshopId] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507112119_InitialServiceRecordStructure'
)
BEGIN
    CREATE TABLE [ServiceOperations] (
        [Id] int NOT NULL IDENTITY,
        [ServiceRecordId] int NOT NULL,
        [Type] int NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [Quantity] int NOT NULL,
        [UnitPrice] decimal(18,2) NOT NULL,
        [TotalPrice] decimal(18,2) NOT NULL,
        [Note] nvarchar(max) NULL,
        CONSTRAINT [PK_ServiceOperations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ServiceOperations_ServiceRecords_ServiceRecordId] FOREIGN KEY ([ServiceRecordId]) REFERENCES [ServiceRecords] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507112119_InitialServiceRecordStructure'
)
BEGIN
    CREATE TABLE [ServiceRecordImages] (
        [Id] int NOT NULL IDENTITY,
        [ServiceRecordId] int NOT NULL,
        [Type] int NOT NULL,
        [FilePath] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_ServiceRecordImages] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ServiceRecordImages_ServiceRecords_ServiceRecordId] FOREIGN KEY ([ServiceRecordId]) REFERENCES [ServiceRecords] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507112119_InitialServiceRecordStructure'
)
BEGIN
    CREATE TABLE [VehicleBrands] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(max) NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_VehicleBrands] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507112119_InitialServiceRecordStructure'
)
BEGIN
    CREATE TABLE [VehicleModels] (
        [Id] int NOT NULL IDENTITY,
        [VehicleBrandId] int NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_VehicleModels] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_VehicleModels_VehicleBrands_VehicleBrandId] FOREIGN KEY ([VehicleBrandId]) REFERENCES [VehicleBrands] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507112119_InitialServiceRecordStructure'
)
BEGIN
    CREATE INDEX [IX_Vehicles_VehicleBrandId] ON [Vehicles] ([VehicleBrandId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507112119_InitialServiceRecordStructure'
)
BEGIN
    CREATE INDEX [IX_Vehicles_VehicleModelId] ON [Vehicles] ([VehicleModelId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507112119_InitialServiceRecordStructure'
)
BEGIN
    CREATE INDEX [IX_ServiceRecords_CustomerId] ON [ServiceRecords] ([CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507112119_InitialServiceRecordStructure'
)
BEGIN
    CREATE INDEX [IX_ServiceOperations_ServiceRecordId] ON [ServiceOperations] ([ServiceRecordId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507112119_InitialServiceRecordStructure'
)
BEGIN
    CREATE INDEX [IX_ServiceRecordImages_ServiceRecordId] ON [ServiceRecordImages] ([ServiceRecordId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507112119_InitialServiceRecordStructure'
)
BEGIN
    CREATE INDEX [IX_VehicleModels_VehicleBrandId] ON [VehicleModels] ([VehicleBrandId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507112119_InitialServiceRecordStructure'
)
BEGIN
    ALTER TABLE [ServiceRecords] ADD CONSTRAINT [FK_ServiceRecords_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507112119_InitialServiceRecordStructure'
)
BEGIN
    ALTER TABLE [ServiceRecords] ADD CONSTRAINT [FK_ServiceRecords_Vehicles_VehicleId] FOREIGN KEY ([VehicleId]) REFERENCES [Vehicles] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507112119_InitialServiceRecordStructure'
)
BEGIN
    ALTER TABLE [Vehicles] ADD CONSTRAINT [FK_Vehicles_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507112119_InitialServiceRecordStructure'
)
BEGIN
    ALTER TABLE [Vehicles] ADD CONSTRAINT [FK_Vehicles_VehicleBrands_VehicleBrandId] FOREIGN KEY ([VehicleBrandId]) REFERENCES [VehicleBrands] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507112119_InitialServiceRecordStructure'
)
BEGIN
    ALTER TABLE [Vehicles] ADD CONSTRAINT [FK_Vehicles_VehicleModels_VehicleModelId] FOREIGN KEY ([VehicleModelId]) REFERENCES [VehicleModels] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507112119_InitialServiceRecordStructure'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260507112119_InitialServiceRecordStructure', N'10.0.7');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507121712_AddServiceRecordNumber'
)
BEGIN
    ALTER TABLE [ServiceRecords] ADD [RecordNumber] nvarchar(50) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507121712_AddServiceRecordNumber'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ServiceRecords_RecordNumber] ON [ServiceRecords] ([RecordNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507121712_AddServiceRecordNumber'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260507121712_AddServiceRecordNumber', N'10.0.7');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507123427_SeedVehicleBrandsAndModels'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'IsActive', N'Name') AND [object_id] = OBJECT_ID(N'[VehicleBrands]'))
        SET IDENTITY_INSERT [VehicleBrands] ON;
    EXEC(N'INSERT INTO [VehicleBrands] ([Id], [IsActive], [Name])
    VALUES (1, CAST(1 AS bit), N''Toyota''),
    (2, CAST(1 AS bit), N''Honda''),
    (3, CAST(1 AS bit), N''Volkswagen''),
    (4, CAST(1 AS bit), N''BMW''),
    (5, CAST(1 AS bit), N''Mercedes-Benz''),
    (6, CAST(1 AS bit), N''Ford''),
    (7, CAST(1 AS bit), N''Renault''),
    (8, CAST(1 AS bit), N''Fiat''),
    (9, CAST(1 AS bit), N''Hyundai''),
    (10, CAST(1 AS bit), N''Peugeot'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'IsActive', N'Name') AND [object_id] = OBJECT_ID(N'[VehicleBrands]'))
        SET IDENTITY_INSERT [VehicleBrands] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507123427_SeedVehicleBrandsAndModels'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'IsActive', N'Name', N'VehicleBrandId') AND [object_id] = OBJECT_ID(N'[VehicleModels]'))
        SET IDENTITY_INSERT [VehicleModels] ON;
    EXEC(N'INSERT INTO [VehicleModels] ([Id], [IsActive], [Name], [VehicleBrandId])
    VALUES (1, CAST(1 AS bit), N''Corolla'', 1),
    (2, CAST(1 AS bit), N''Yaris'', 1),
    (3, CAST(1 AS bit), N''C-HR'', 1),
    (4, CAST(1 AS bit), N''Civic'', 2),
    (5, CAST(1 AS bit), N''Jazz'', 2),
    (6, CAST(1 AS bit), N''CR-V'', 2),
    (7, CAST(1 AS bit), N''Golf'', 3),
    (8, CAST(1 AS bit), N''Passat'', 3),
    (9, CAST(1 AS bit), N''Polo'', 3),
    (10, CAST(1 AS bit), N''3 Series'', 4),
    (11, CAST(1 AS bit), N''5 Series'', 4),
    (12, CAST(1 AS bit), N''X5'', 4),
    (13, CAST(1 AS bit), N''C-Class'', 5),
    (14, CAST(1 AS bit), N''E-Class'', 5),
    (15, CAST(1 AS bit), N''Sprinter'', 5),
    (16, CAST(1 AS bit), N''Focus'', 6),
    (17, CAST(1 AS bit), N''Fiesta'', 6),
    (18, CAST(1 AS bit), N''Transit'', 6),
    (19, CAST(1 AS bit), N''Clio'', 7),
    (20, CAST(1 AS bit), N''Megane'', 7),
    (21, CAST(1 AS bit), N''Fluence'', 7),
    (22, CAST(1 AS bit), N''Egea'', 8),
    (23, CAST(1 AS bit), N''Linea'', 8),
    (24, CAST(1 AS bit), N''Doblo'', 8),
    (25, CAST(1 AS bit), N''i20'', 9),
    (26, CAST(1 AS bit), N''i30'', 9),
    (27, CAST(1 AS bit), N''Tucson'', 9),
    (28, CAST(1 AS bit), N''208'', 10),
    (29, CAST(1 AS bit), N''308'', 10),
    (30, CAST(1 AS bit), N''2008'', 10)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'IsActive', N'Name', N'VehicleBrandId') AND [object_id] = OBJECT_ID(N'[VehicleModels]'))
        SET IDENTITY_INSERT [VehicleModels] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507123427_SeedVehicleBrandsAndModels'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260507123427_SeedVehicleBrandsAndModels', N'10.0.7');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507180555_AddServiceRequestItemsAndEstimatedAmount'
)
BEGIN
    ALTER TABLE [ServiceRecords] ADD [EstimatedAmount] decimal(18,2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507180555_AddServiceRequestItemsAndEstimatedAmount'
)
BEGIN
    ALTER TABLE [ServiceRecords] ADD [EstimatedAmountNote] nvarchar(500) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507180555_AddServiceRequestItemsAndEstimatedAmount'
)
BEGIN
    CREATE TABLE [ServiceRequestItems] (
        [Id] int NOT NULL IDENTITY,
        [ServiceRecordId] int NOT NULL,
        [Title] nvarchar(250) NOT NULL,
        [Note] nvarchar(1000) NULL,
        [RepairDetail] nvarchar(2000) NULL,
        [IsResolved] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_ServiceRequestItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ServiceRequestItems_ServiceRecords_ServiceRecordId] FOREIGN KEY ([ServiceRecordId]) REFERENCES [ServiceRecords] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507180555_AddServiceRequestItemsAndEstimatedAmount'
)
BEGIN
    CREATE INDEX [IX_ServiceRequestItems_ServiceRecordId] ON [ServiceRequestItems] ([ServiceRecordId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507180555_AddServiceRequestItemsAndEstimatedAmount'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260507180555_AddServiceRequestItemsAndEstimatedAmount', N'10.0.7');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507185917_AddAmountsToServiceRequestItems'
)
BEGIN
    ALTER TABLE [ServiceRequestItems] ADD [EstimatedAmount] decimal(18,2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507185917_AddAmountsToServiceRequestItems'
)
BEGIN
    ALTER TABLE [ServiceRequestItems] ADD [FinalAmount] decimal(18,2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507185917_AddAmountsToServiceRequestItems'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260507185917_AddAmountsToServiceRequestItems', N'10.0.7');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260510193224_LinkServiceOperationsToRequestItems'
)
BEGIN
    DECLARE @var9 nvarchar(max);
    SELECT @var9 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ServiceOperations]') AND [c].[name] = N'Note');
    IF @var9 IS NOT NULL EXEC(N'ALTER TABLE [ServiceOperations] DROP CONSTRAINT ' + @var9 + ';');
    ALTER TABLE [ServiceOperations] ALTER COLUMN [Note] nvarchar(1000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260510193224_LinkServiceOperationsToRequestItems'
)
BEGIN
    DECLARE @var10 nvarchar(max);
    SELECT @var10 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ServiceOperations]') AND [c].[name] = N'Description');
    IF @var10 IS NOT NULL EXEC(N'ALTER TABLE [ServiceOperations] DROP CONSTRAINT ' + @var10 + ';');
    ALTER TABLE [ServiceOperations] ALTER COLUMN [Description] nvarchar(250) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260510193224_LinkServiceOperationsToRequestItems'
)
BEGIN
    ALTER TABLE [ServiceOperations] ADD [ServiceRequestItemId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260510193224_LinkServiceOperationsToRequestItems'
)
BEGIN
    CREATE INDEX [IX_ServiceOperations_ServiceRequestItemId] ON [ServiceOperations] ([ServiceRequestItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260510193224_LinkServiceOperationsToRequestItems'
)
BEGIN
    ALTER TABLE [ServiceOperations] ADD CONSTRAINT [FK_ServiceOperations_ServiceRequestItems_ServiceRequestItemId] FOREIGN KEY ([ServiceRequestItemId]) REFERENCES [ServiceRequestItems] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260510193224_LinkServiceOperationsToRequestItems'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260510193224_LinkServiceOperationsToRequestItems', N'10.0.7');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512103015_AddVehicleQrCodes'
)
BEGIN
    CREATE TABLE [VehicleQrCodes] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(max) NOT NULL,
        [WorkshopId] int NULL,
        [VehicleId] int NULL,
        [IsAssigned] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [AssignedAt] datetime2 NULL,
        CONSTRAINT [PK_VehicleQrCodes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_VehicleQrCodes_Vehicles_VehicleId] FOREIGN KEY ([VehicleId]) REFERENCES [Vehicles] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512103015_AddVehicleQrCodes'
)
BEGIN
    CREATE INDEX [IX_VehicleQrCodes_VehicleId] ON [VehicleQrCodes] ([VehicleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512103015_AddVehicleQrCodes'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260512103015_AddVehicleQrCodes', N'10.0.7');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512170205_AddChassisNumberToVehicles'
)
BEGIN
    ALTER TABLE [Vehicles] ADD [ChassisNumber] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512170205_AddChassisNumberToVehicles'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260512170205_AddChassisNumberToVehicles', N'10.0.7');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512195937_AddUpdatedAtToServiceRecords'
)
BEGIN
    ALTER TABLE [ServiceRecords] ADD [UpdatedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512195937_AddUpdatedAtToServiceRecords'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260512195937_AddUpdatedAtToServiceRecords', N'10.0.7');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260516170622_AddVehicleDeliveredByToServiceRecord'
)
BEGIN
    ALTER TABLE [ServiceRecords] ADD [VehicleDeliveredBySnapshot] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260516170622_AddVehicleDeliveredByToServiceRecord'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260516170622_AddVehicleDeliveredByToServiceRecord', N'10.0.7');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260516183318_AddCustomerAddressFields'
)
BEGIN
    ALTER TABLE [Customers] ADD [AddressCity] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260516183318_AddCustomerAddressFields'
)
BEGIN
    ALTER TABLE [Customers] ADD [AddressDistrict] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260516183318_AddCustomerAddressFields'
)
BEGIN
    ALTER TABLE [Customers] ADD [NationalIdentityNumber] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260516183318_AddCustomerAddressFields'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260516183318_AddCustomerAddressFields', N'10.0.7');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260517154641_AddInvoiceTables'
)
BEGIN
    CREATE TABLE [Invoices] (
        [Id] int NOT NULL IDENTITY,
        [WorkshopId] int NOT NULL,
        [CustomerId] int NOT NULL,
        [ServiceRecordId] int NULL,
        [Type] int NOT NULL,
        [Status] int NOT NULL,
        [InvoiceNumber] nvarchar(50) NOT NULL,
        [InvoiceDate] datetime2 NOT NULL,
        [CustomerTitle] nvarchar(200) NOT NULL,
        [CustomerTaxOffice] nvarchar(100) NULL,
        [CustomerTaxNumber] nvarchar(50) NULL,
        [CustomerTckn] nvarchar(11) NULL,
        [CustomerAddress] nvarchar(500) NULL,
        [Plate] nvarchar(20) NULL,
        [ChassisNumber] nvarchar(100) NULL,
        [Mileage] int NULL,
        [Subtotal] decimal(18,2) NOT NULL,
        [DiscountTotal] decimal(18,2) NOT NULL,
        [VatTotal] decimal(18,2) NOT NULL,
        [GrandTotal] decimal(18,2) NOT NULL,
        [Notes] nvarchar(max) NULL,
        [ExternalInvoiceId] nvarchar(100) NULL,
        [ExternalInvoiceNumber] nvarchar(100) NULL,
        [ExternalUuid] nvarchar(200) NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        CONSTRAINT [PK_Invoices] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Invoices_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Invoices_ServiceRecords_ServiceRecordId] FOREIGN KEY ([ServiceRecordId]) REFERENCES [ServiceRecords] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260517154641_AddInvoiceTables'
)
BEGIN
    CREATE TABLE [InvoiceItems] (
        [Id] int NOT NULL IDENTITY,
        [InvoiceId] int NOT NULL,
        [ItemType] int NOT NULL,
        [Description] nvarchar(500) NOT NULL,
        [Quantity] decimal(18,2) NOT NULL,
        [Unit] nvarchar(20) NOT NULL,
        [UnitPrice] decimal(18,2) NOT NULL,
        [DiscountRate] decimal(18,2) NOT NULL,
        [DiscountAmount] decimal(18,2) NOT NULL,
        [VatRate] decimal(18,2) NOT NULL,
        [VatAmount] decimal(18,2) NOT NULL,
        [LineTotal] decimal(18,2) NOT NULL,
        CONSTRAINT [PK_InvoiceItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_InvoiceItems_Invoices_InvoiceId] FOREIGN KEY ([InvoiceId]) REFERENCES [Invoices] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260517154641_AddInvoiceTables'
)
BEGIN
    CREATE INDEX [IX_InvoiceItems_InvoiceId] ON [InvoiceItems] ([InvoiceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260517154641_AddInvoiceTables'
)
BEGIN
    CREATE INDEX [IX_Invoices_CustomerId] ON [Invoices] ([CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260517154641_AddInvoiceTables'
)
BEGIN
    CREATE INDEX [IX_Invoices_ServiceRecordId] ON [Invoices] ([ServiceRecordId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260517154641_AddInvoiceTables'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260517154641_AddInvoiceTables', N'10.0.7');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260521114624_AddCurrentAccountTransactions'
)
BEGIN
    CREATE TABLE [CurrentAccountTransactions] (
        [Id] int NOT NULL IDENTITY,
        [WorkshopId] int NOT NULL,
        [CustomerId] int NOT NULL,
        [InvoiceId] int NULL,
        [Type] int NOT NULL,
        [Debit] decimal(18,2) NOT NULL,
        [Credit] decimal(18,2) NOT NULL,
        [TransactionDate] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [Description] nvarchar(500) NOT NULL,
        [DocumentNumber] nvarchar(100) NULL,
        [IsSystemGenerated] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        CONSTRAINT [PK_CurrentAccountTransactions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CurrentAccountTransactions_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CurrentAccountTransactions_Invoices_InvoiceId] FOREIGN KEY ([InvoiceId]) REFERENCES [Invoices] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260521114624_AddCurrentAccountTransactions'
)
BEGIN
    CREATE INDEX [IX_CurrentAccountTransactions_CustomerId] ON [CurrentAccountTransactions] ([CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260521114624_AddCurrentAccountTransactions'
)
BEGIN
    CREATE INDEX [IX_CurrentAccountTransactions_InvoiceId] ON [CurrentAccountTransactions] ([InvoiceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260521114624_AddCurrentAccountTransactions'
)
BEGIN
    CREATE INDEX [IX_CurrentAccountTransactions_WorkshopId_CustomerId_TransactionDate] ON [CurrentAccountTransactions] ([WorkshopId], [CustomerId], [TransactionDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260521114624_AddCurrentAccountTransactions'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260521114624_AddCurrentAccountTransactions', N'10.0.7');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260521205253_UpdateCustomerTypes'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260521205253_UpdateCustomerTypes', N'10.0.7');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523123150_AddStockModule'
)
BEGIN
    CREATE TABLE [StockItems] (
        [Id] int NOT NULL IDENTITY,
        [WorkshopId] int NOT NULL,
        [Name] nvarchar(150) NOT NULL,
        [Code] nvarchar(50) NULL,
        [Barcode] nvarchar(100) NULL,
        [Brand] nvarchar(100) NULL,
        [Unit] nvarchar(20) NOT NULL,
        [Quantity] decimal(18,2) NOT NULL,
        [PurchasePrice] decimal(18,2) NOT NULL,
        [SalePrice] decimal(18,2) NOT NULL,
        [MinimumQuantity] decimal(18,2) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        CONSTRAINT [PK_StockItems] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523123150_AddStockModule'
)
BEGIN
    CREATE TABLE [StockMovements] (
        [Id] int NOT NULL IDENTITY,
        [WorkshopId] int NOT NULL,
        [StockItemId] int NOT NULL,
        [MovementType] int NOT NULL,
        [Quantity] decimal(18,2) NOT NULL,
        [UnitPrice] decimal(18,2) NULL,
        [Description] nvarchar(500) NULL,
        [ReferenceType] nvarchar(50) NULL,
        [ReferenceId] int NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [CreatedByUserId] int NULL,
        CONSTRAINT [PK_StockMovements] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_StockMovements_StockItems_StockItemId] FOREIGN KEY ([StockItemId]) REFERENCES [StockItems] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523123150_AddStockModule'
)
BEGIN
    CREATE INDEX [IX_StockItems_WorkshopId_Barcode] ON [StockItems] ([WorkshopId], [Barcode]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523123150_AddStockModule'
)
BEGIN
    CREATE INDEX [IX_StockItems_WorkshopId_Code] ON [StockItems] ([WorkshopId], [Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523123150_AddStockModule'
)
BEGIN
    CREATE INDEX [IX_StockItems_WorkshopId_Name] ON [StockItems] ([WorkshopId], [Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523123150_AddStockModule'
)
BEGIN
    CREATE INDEX [IX_StockMovements_ReferenceType_ReferenceId] ON [StockMovements] ([ReferenceType], [ReferenceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523123150_AddStockModule'
)
BEGIN
    CREATE INDEX [IX_StockMovements_StockItemId] ON [StockMovements] ([StockItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523123150_AddStockModule'
)
BEGIN
    CREATE INDEX [IX_StockMovements_WorkshopId_StockItemId_CreatedAt] ON [StockMovements] ([WorkshopId], [StockItemId], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523123150_AddStockModule'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260523123150_AddStockModule', N'10.0.7');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523161437_AddStockItemRelationToInvoiceItem'
)
BEGIN
    ALTER TABLE [InvoiceItems] ADD [StockItemId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523161437_AddStockItemRelationToInvoiceItem'
)
BEGIN
    CREATE INDEX [IX_InvoiceItems_StockItemId] ON [InvoiceItems] ([StockItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523161437_AddStockItemRelationToInvoiceItem'
)
BEGIN
    ALTER TABLE [InvoiceItems] ADD CONSTRAINT [FK_InvoiceItems_StockItems_StockItemId] FOREIGN KEY ([StockItemId]) REFERENCES [StockItems] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523161437_AddStockItemRelationToInvoiceItem'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260523161437_AddStockItemRelationToInvoiceItem', N'10.0.7');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523163154_AddStockItemToServiceOperation'
)
BEGIN
    ALTER TABLE [ServiceOperations] ADD [StockItemId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523163154_AddStockItemToServiceOperation'
)
BEGIN
    CREATE INDEX [IX_ServiceOperations_StockItemId] ON [ServiceOperations] ([StockItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523163154_AddStockItemToServiceOperation'
)
BEGIN
    ALTER TABLE [ServiceOperations] ADD CONSTRAINT [FK_ServiceOperations_StockItems_StockItemId] FOREIGN KEY ([StockItemId]) REFERENCES [StockItems] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523163154_AddStockItemToServiceOperation'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260523163154_AddStockItemToServiceOperation', N'10.0.7');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524174750_AddSubscriptionFieldsToWorkshop'
)
BEGIN
    ALTER TABLE [Workshops] ADD [IsActive] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524174750_AddSubscriptionFieldsToWorkshop'
)
BEGIN
    ALTER TABLE [Workshops] ADD [SubscriptionEndDate] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524174750_AddSubscriptionFieldsToWorkshop'
)
BEGIN
    ALTER TABLE [Workshops] ADD [SubscriptionNote] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524174750_AddSubscriptionFieldsToWorkshop'
)
BEGIN
    ALTER TABLE [Workshops] ADD [SubscriptionStartDate] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524174750_AddSubscriptionFieldsToWorkshop'
)
BEGIN
    ALTER TABLE [Workshops] ADD [SubscriptionStatus] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524174750_AddSubscriptionFieldsToWorkshop'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260524174750_AddSubscriptionFieldsToWorkshop', N'10.0.7');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524193221_AddWorkshopProfileAndPartners'
)
BEGIN
    CREATE TABLE [WorkshopPartners] (
        [Id] int NOT NULL IDENTITY,
        [WorkshopId] int NOT NULL,
        [FullName] nvarchar(150) NOT NULL,
        [Title] nvarchar(100) NULL,
        [PhoneNumber] nvarchar(30) NULL,
        [Email] nvarchar(150) NULL,
        [IsPrimary] bit NOT NULL,
        [Note] nvarchar(500) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_WorkshopPartners] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_WorkshopPartners_Workshops_WorkshopId] FOREIGN KEY ([WorkshopId]) REFERENCES [Workshops] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524193221_AddWorkshopProfileAndPartners'
)
BEGIN
    CREATE TABLE [WorkshopProfiles] (
        [Id] int NOT NULL IDENTITY,
        [WorkshopId] int NOT NULL,
        [DisplayName] nvarchar(200) NULL,
        [LegalTitle] nvarchar(300) NULL,
        [TaxOffice] nvarchar(100) NULL,
        [TaxNumber] nvarchar(20) NULL,
        [TradeRegistryNumber] nvarchar(50) NULL,
        [MersisNumber] nvarchar(50) NULL,
        [Email] nvarchar(150) NULL,
        [PhoneNumber] nvarchar(30) NULL,
        [FaxNumber] nvarchar(30) NULL,
        [Website] nvarchar(150) NULL,
        [AddressLine] nvarchar(500) NULL,
        [City] nvarchar(100) NULL,
        [District] nvarchar(100) NULL,
        [PostalCode] nvarchar(20) NULL,
        [Country] nvarchar(100) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_WorkshopProfiles] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_WorkshopProfiles_Workshops_WorkshopId] FOREIGN KEY ([WorkshopId]) REFERENCES [Workshops] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524193221_AddWorkshopProfileAndPartners'
)
BEGIN
    CREATE INDEX [IX_WorkshopPartners_WorkshopId] ON [WorkshopPartners] ([WorkshopId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524193221_AddWorkshopProfileAndPartners'
)
BEGIN
    CREATE UNIQUE INDEX [IX_WorkshopProfiles_WorkshopId] ON [WorkshopProfiles] ([WorkshopId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260524193221_AddWorkshopProfileAndPartners'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260524193221_AddWorkshopProfileAndPartners', N'10.0.7');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260601190026_RepairProductionAdminSchema'
)
BEGIN

    IF COL_LENGTH('dbo.Workshops', 'IsActive') IS NULL
    BEGIN
        ALTER TABLE dbo.Workshops
        ADD IsActive bit NOT NULL
            CONSTRAINT DF_Workshops_IsActive DEFAULT(1);
    END;

    IF COL_LENGTH('dbo.Workshops', 'SubscriptionStatus') IS NULL
    BEGIN
        ALTER TABLE dbo.Workshops
        ADD SubscriptionStatus int NOT NULL
            CONSTRAINT DF_Workshops_SubscriptionStatus DEFAULT(1);
    END;

    IF COL_LENGTH('dbo.Workshops', 'SubscriptionStartDate') IS NULL
    BEGIN
        ALTER TABLE dbo.Workshops
        ADD SubscriptionStartDate datetime2 NULL;

        UPDATE dbo.Workshops
        SET SubscriptionStartDate = ISNULL(CreatedAt, SYSUTCDATETIME())
        WHERE SubscriptionStartDate IS NULL;

        ALTER TABLE dbo.Workshops
        ALTER COLUMN SubscriptionStartDate datetime2 NOT NULL;

        ALTER TABLE dbo.Workshops
        ADD CONSTRAINT DF_Workshops_SubscriptionStartDate
        DEFAULT(SYSUTCDATETIME()) FOR SubscriptionStartDate;
    END;

    IF COL_LENGTH('dbo.Workshops', 'SubscriptionEndDate') IS NULL
    BEGIN
        ALTER TABLE dbo.Workshops
        ADD SubscriptionEndDate datetime2 NULL;
    END;

    IF COL_LENGTH('dbo.Workshops', 'SubscriptionNote') IS NULL
    BEGIN
        ALTER TABLE dbo.Workshops
        ADD SubscriptionNote nvarchar(max) NULL;
    END;

    IF OBJECT_ID('dbo.WorkshopProfiles', 'U') IS NULL
    BEGIN
        CREATE TABLE dbo.WorkshopProfiles
        (
            Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_WorkshopProfiles PRIMARY KEY,
            WorkshopId int NOT NULL,
            DisplayName nvarchar(200) NULL,
            LegalTitle nvarchar(250) NULL,
            TaxOffice nvarchar(100) NULL,
            TaxNumber nvarchar(50) NULL,
            TradeRegistryNumber nvarchar(100) NULL,
            MersisNumber nvarchar(100) NULL,
            Email nvarchar(150) NULL,
            PhoneNumber nvarchar(30) NULL,
            FaxNumber nvarchar(30) NULL,
            Website nvarchar(200) NULL,
            AddressLine nvarchar(500) NULL,
            City nvarchar(100) NULL,
            District nvarchar(100) NULL,
            PostalCode nvarchar(20) NULL,
            Country nvarchar(100) NULL,
            CreatedAt datetime2 NOT NULL,
            UpdatedAt datetime2 NULL,
            CONSTRAINT FK_WorkshopProfiles_Workshops_WorkshopId
                FOREIGN KEY (WorkshopId) REFERENCES dbo.Workshops(Id) ON DELETE CASCADE
        );
    END;

    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes 
        WHERE name = 'IX_WorkshopProfiles_WorkshopId'
          AND object_id = OBJECT_ID('dbo.WorkshopProfiles')
    )
    BEGIN
        CREATE UNIQUE INDEX IX_WorkshopProfiles_WorkshopId
        ON dbo.WorkshopProfiles(WorkshopId);
    END;

    IF OBJECT_ID('dbo.WorkshopPartners', 'U') IS NULL
    BEGIN
        CREATE TABLE dbo.WorkshopPartners
        (
            Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_WorkshopPartners PRIMARY KEY,
            WorkshopId int NOT NULL,
            FullName nvarchar(200) NOT NULL,
            Title nvarchar(100) NULL,
            PhoneNumber nvarchar(30) NULL,
            Email nvarchar(150) NULL,
            IsPrimary bit NOT NULL CONSTRAINT DF_WorkshopPartners_IsPrimary DEFAULT(0),
            Note nvarchar(500) NULL,
            CreatedAt datetime2 NOT NULL,
            CONSTRAINT FK_WorkshopPartners_Workshops_WorkshopId
                FOREIGN KEY (WorkshopId) REFERENCES dbo.Workshops(Id) ON DELETE CASCADE
        );
    END;

    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes 
        WHERE name = 'IX_WorkshopPartners_WorkshopId'
          AND object_id = OBJECT_ID('dbo.WorkshopPartners')
    )
    BEGIN
        CREATE INDEX IX_WorkshopPartners_WorkshopId
        ON dbo.WorkshopPartners(WorkshopId);
    END;

    UPDATE dbo.Workshops
    SET
        IsActive = 1,
        SubscriptionStatus = CASE WHEN SubscriptionStatus = 0 THEN 2 ELSE SubscriptionStatus END,
        SubscriptionStartDate = ISNULL(SubscriptionStartDate, CreatedAt)
    WHERE IsActive = 0
       OR SubscriptionStatus = 0
       OR SubscriptionStartDate IS NULL;

    INSERT INTO dbo.WorkshopProfiles
    (
        WorkshopId,
        DisplayName,
        Country,
        CreatedAt
    )
    SELECT
        w.Id,
        w.Name,
        N'Türkiye',
        SYSUTCDATETIME()
    FROM dbo.Workshops w
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.WorkshopProfiles p
        WHERE p.WorkshopId = w.Id
    );

END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260601190026_RepairProductionAdminSchema'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260601190026_RepairProductionAdminSchema', N'10.0.7');
END;

COMMIT;
GO

