using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoStock.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddStockItemToServiceOperation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StockItemId",
                table: "ServiceOperations",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceOperations_StockItemId",
                table: "ServiceOperations",
                column: "StockItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceOperations_StockItems_StockItemId",
                table: "ServiceOperations",
                column: "StockItemId",
                principalTable: "StockItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ServiceOperations_StockItems_StockItemId",
                table: "ServiceOperations");

            migrationBuilder.DropIndex(
                name: "IX_ServiceOperations_StockItemId",
                table: "ServiceOperations");

            migrationBuilder.DropColumn(
                name: "StockItemId",
                table: "ServiceOperations");
        }
    }
}
