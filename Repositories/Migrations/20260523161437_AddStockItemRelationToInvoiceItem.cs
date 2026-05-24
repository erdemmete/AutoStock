using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoStock.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddStockItemRelationToInvoiceItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StockItemId",
                table: "InvoiceItems",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceItems_StockItemId",
                table: "InvoiceItems",
                column: "StockItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_InvoiceItems_StockItems_StockItemId",
                table: "InvoiceItems",
                column: "StockItemId",
                principalTable: "StockItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InvoiceItems_StockItems_StockItemId",
                table: "InvoiceItems");

            migrationBuilder.DropIndex(
                name: "IX_InvoiceItems_StockItemId",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "StockItemId",
                table: "InvoiceItems");
        }
    }
}
