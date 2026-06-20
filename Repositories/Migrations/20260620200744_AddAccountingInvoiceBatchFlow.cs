using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoStock.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountingInvoiceBatchFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CustomerDeliveredAt",
                table: "OfficialInvoiceDocuments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CustomerDeliveredByUserId",
                table: "OfficialInvoiceDocuments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerDeliveryChannel",
                table: "OfficialInvoiceDocuments",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShareToken",
                table: "OfficialInvoiceDocuments",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE OfficialInvoiceDocuments
                SET ShareToken = LOWER(REPLACE(CONVERT(nvarchar(36), NEWID()), '-', '') + REPLACE(CONVERT(nvarchar(36), NEWID()), '-', ''))
                WHERE ShareToken IS NULL OR LTRIM(RTRIM(ShareToken)) = ''
                """);

            migrationBuilder.AlterColumn<string>(
                name: "ShareToken",
                table: "OfficialInvoiceDocuments",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128,
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "BatchCompletedAt",
                table: "AccountingInvoiceRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BatchToken",
                table: "AccountingInvoiceRequests",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RequestedByUserId",
                table: "AccountingInvoiceRequests",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OfficialInvoiceDocuments_ShareToken",
                table: "OfficialInvoiceDocuments",
                column: "ShareToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountingInvoiceRequests_BatchToken",
                table: "AccountingInvoiceRequests",
                column: "BatchToken");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OfficialInvoiceDocuments_ShareToken",
                table: "OfficialInvoiceDocuments");

            migrationBuilder.DropIndex(
                name: "IX_AccountingInvoiceRequests_BatchToken",
                table: "AccountingInvoiceRequests");

            migrationBuilder.DropColumn(
                name: "CustomerDeliveredAt",
                table: "OfficialInvoiceDocuments");

            migrationBuilder.DropColumn(
                name: "CustomerDeliveredByUserId",
                table: "OfficialInvoiceDocuments");

            migrationBuilder.DropColumn(
                name: "CustomerDeliveryChannel",
                table: "OfficialInvoiceDocuments");

            migrationBuilder.DropColumn(
                name: "ShareToken",
                table: "OfficialInvoiceDocuments");

            migrationBuilder.DropColumn(
                name: "BatchCompletedAt",
                table: "AccountingInvoiceRequests");

            migrationBuilder.DropColumn(
                name: "BatchToken",
                table: "AccountingInvoiceRequests");

            migrationBuilder.DropColumn(
                name: "RequestedByUserId",
                table: "AccountingInvoiceRequests");
        }
    }
}
