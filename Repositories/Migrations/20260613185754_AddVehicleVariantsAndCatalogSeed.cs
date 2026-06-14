using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoStock.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleVariantsAndCatalogSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BodyType",
                table: "Vehicles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EngineCapacityCc",
                table: "Vehicles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EngineCode",
                table: "Vehicles",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EnginePowerHp",
                table: "Vehicles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FuelType",
                table: "Vehicles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TransmissionType",
                table: "Vehicles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VehicleVariantId",
                table: "Vehicles",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "VehicleVariants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VehicleBrandId = table.Column<int>(type: "int", nullable: false),
                    VehicleModelId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FuelType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TransmissionType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    BodyType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    EngineCapacityCc = table.Column<int>(type: "int", nullable: true),
                    EnginePowerHp = table.Column<int>(type: "int", nullable: true),
                    EngineCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    ModelYearFrom = table.Column<int>(type: "int", nullable: true),
                    ModelYearTo = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleVariants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VehicleVariants_VehicleBrands_VehicleBrandId",
                        column: x => x.VehicleBrandId,
                        principalTable: "VehicleBrands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehicleVariants_VehicleModels_VehicleModelId",
                        column: x => x.VehicleModelId,
                        principalTable: "VehicleModels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_VehicleVariantId",
                table: "Vehicles",
                column: "VehicleVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleVariants_VehicleBrandId",
                table: "VehicleVariants",
                column: "VehicleBrandId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleVariants_VehicleModelId",
                table: "VehicleVariants",
                column: "VehicleModelId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleVariants_VehicleModelId_Name",
                table: "VehicleVariants",
                columns: new[] { "VehicleModelId", "Name" });

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicles_VehicleVariants_VehicleVariantId",
                table: "Vehicles",
                column: "VehicleVariantId",
                principalTable: "VehicleVariants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_VehicleVariants_VehicleVariantId",
                table: "Vehicles");

            migrationBuilder.DropTable(
                name: "VehicleVariants");

            migrationBuilder.DropIndex(
                name: "IX_Vehicles_VehicleVariantId",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "BodyType",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "EngineCapacityCc",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "EngineCode",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "EnginePowerHp",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "FuelType",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "TransmissionType",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "VehicleVariantId",
                table: "Vehicles");
        }
    }
}
