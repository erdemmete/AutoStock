using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoStock.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class RepairProductionAdminSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
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
");
        }
        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
