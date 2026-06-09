using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoStock.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddCodeHashToUserSecurityTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CodeHash",
                table: "UserSecurityTokens",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSecurityTokens_CodeHash",
                table: "UserSecurityTokens",
                column: "CodeHash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserSecurityTokens_CodeHash",
                table: "UserSecurityTokens");

            migrationBuilder.DropColumn(
                name: "CodeHash",
                table: "UserSecurityTokens");
        }
    }
}
