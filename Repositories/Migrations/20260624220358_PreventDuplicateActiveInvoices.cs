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
            migrationBuilder.DropIndex(
                name: "IX_Invoices_ServiceRecordId",
                table: "Invoices");

            migrationBuilder.AddColumn<string>(
                name: "ClientRequestId",
                table: "ServiceRecords",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

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
