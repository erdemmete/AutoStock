using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoStock.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleDeliveredByToServiceRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VehicleDeliveredBySnapshot",
                table: "ServiceRecords",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VehicleDeliveredBySnapshot",
                table: "ServiceRecords");
        }
    }
}
