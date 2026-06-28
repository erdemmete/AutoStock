using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoStock.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class PreventDuplicateActiveInvoices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClientRequestId",
                table: "ServiceRecords",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.Sql("""
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
                """);

            migrationBuilder.Sql("""
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
                """);

            migrationBuilder.DropIndex(
                name: "IX_Invoices_ServiceRecordId",
                table: "Invoices");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRecords_WorkshopId_ClientRequestId",
                table: "ServiceRecords",
                columns: new[] { "WorkshopId", "ClientRequestId" },
                unique: true,
                filter: "[ClientRequestId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_Invoices_Active_ServiceRecordId",
                table: "Invoices",
                column: "ServiceRecordId",
                unique: true,
                filter: "[ServiceRecordId] IS NOT NULL AND [Status] IN (1, 2)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ServiceRecords_WorkshopId_ClientRequestId",
                table: "ServiceRecords");

            migrationBuilder.DropIndex(
                name: "UX_Invoices_Active_ServiceRecordId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "ClientRequestId",
                table: "ServiceRecords");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_ServiceRecordId",
                table: "Invoices",
                column: "ServiceRecordId");
        }
    }
}
