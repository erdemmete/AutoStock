using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoStock.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerAddressFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AddressCity",
                table: "Customers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AddressDistrict",
                table: "Customers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NationalIdentityNumber",
                table: "Customers",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AddressCity",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "AddressDistrict",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "NationalIdentityNumber",
                table: "Customers");
        }
    }
}
