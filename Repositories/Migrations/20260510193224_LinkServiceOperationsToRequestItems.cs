using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoStock.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class LinkServiceOperationsToRequestItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Note",
                table: "ServiceOperations",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "ServiceOperations",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<int>(
                name: "ServiceRequestItemId",
                table: "ServiceOperations",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceOperations_ServiceRequestItemId",
                table: "ServiceOperations",
                column: "ServiceRequestItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceOperations_ServiceRequestItems_ServiceRequestItemId",
                table: "ServiceOperations",
                column: "ServiceRequestItemId",
                principalTable: "ServiceRequestItems",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ServiceOperations_ServiceRequestItems_ServiceRequestItemId",
                table: "ServiceOperations");

            migrationBuilder.DropIndex(
                name: "IX_ServiceOperations_ServiceRequestItemId",
                table: "ServiceOperations");

            migrationBuilder.DropColumn(
                name: "ServiceRequestItemId",
                table: "ServiceOperations");

            migrationBuilder.AlterColumn<string>(
                name: "Note",
                table: "ServiceOperations",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "ServiceOperations",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(250)",
                oldMaxLength: 250);
        }
    }
}
