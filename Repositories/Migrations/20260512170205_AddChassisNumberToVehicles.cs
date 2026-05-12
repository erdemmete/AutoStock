using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoStock.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddChassisNumberToVehicles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ChassisNumber",
                table: "Vehicles",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChassisNumber",
                table: "Vehicles");
        }
    }
}
