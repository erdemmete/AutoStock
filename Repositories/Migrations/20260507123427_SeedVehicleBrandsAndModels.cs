using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AutoStock.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class SeedVehicleBrandsAndModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "VehicleBrands",
                columns: new[] { "Id", "IsActive", "Name" },
                values: new object[,]
                {
                    { 1, true, "Toyota" },
                    { 2, true, "Honda" },
                    { 3, true, "Volkswagen" },
                    { 4, true, "BMW" },
                    { 5, true, "Mercedes-Benz" },
                    { 6, true, "Ford" },
                    { 7, true, "Renault" },
                    { 8, true, "Fiat" },
                    { 9, true, "Hyundai" },
                    { 10, true, "Peugeot" }
                });

            migrationBuilder.InsertData(
                table: "VehicleModels",
                columns: new[] { "Id", "IsActive", "Name", "VehicleBrandId" },
                values: new object[,]
                {
                    { 1, true, "Corolla", 1 },
                    { 2, true, "Yaris", 1 },
                    { 3, true, "C-HR", 1 },
                    { 4, true, "Civic", 2 },
                    { 5, true, "Jazz", 2 },
                    { 6, true, "CR-V", 2 },
                    { 7, true, "Golf", 3 },
                    { 8, true, "Passat", 3 },
                    { 9, true, "Polo", 3 },
                    { 10, true, "3 Series", 4 },
                    { 11, true, "5 Series", 4 },
                    { 12, true, "X5", 4 },
                    { 13, true, "C-Class", 5 },
                    { 14, true, "E-Class", 5 },
                    { 15, true, "Sprinter", 5 },
                    { 16, true, "Focus", 6 },
                    { 17, true, "Fiesta", 6 },
                    { 18, true, "Transit", 6 },
                    { 19, true, "Clio", 7 },
                    { 20, true, "Megane", 7 },
                    { 21, true, "Fluence", 7 },
                    { 22, true, "Egea", 8 },
                    { 23, true, "Linea", 8 },
                    { 24, true, "Doblo", 8 },
                    { 25, true, "i20", 9 },
                    { 26, true, "i30", 9 },
                    { 27, true, "Tucson", 9 },
                    { 28, true, "208", 10 },
                    { 29, true, "308", 10 },
                    { 30, true, "2008", 10 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "VehicleModels",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "VehicleModels",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "VehicleModels",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "VehicleModels",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "VehicleModels",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "VehicleModels",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "VehicleModels",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "VehicleModels",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "VehicleModels",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "VehicleModels",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "VehicleModels",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "VehicleModels",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "VehicleModels",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "VehicleModels",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "VehicleModels",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "VehicleModels",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "VehicleModels",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "VehicleModels",
                keyColumn: "Id",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "VehicleModels",
                keyColumn: "Id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "VehicleModels",
                keyColumn: "Id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "VehicleModels",
                keyColumn: "Id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "VehicleModels",
                keyColumn: "Id",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "VehicleModels",
                keyColumn: "Id",
                keyValue: 23);

            migrationBuilder.DeleteData(
                table: "VehicleModels",
                keyColumn: "Id",
                keyValue: 24);

            migrationBuilder.DeleteData(
                table: "VehicleModels",
                keyColumn: "Id",
                keyValue: 25);

            migrationBuilder.DeleteData(
                table: "VehicleModels",
                keyColumn: "Id",
                keyValue: 26);

            migrationBuilder.DeleteData(
                table: "VehicleModels",
                keyColumn: "Id",
                keyValue: 27);

            migrationBuilder.DeleteData(
                table: "VehicleModels",
                keyColumn: "Id",
                keyValue: 28);

            migrationBuilder.DeleteData(
                table: "VehicleModels",
                keyColumn: "Id",
                keyValue: 29);

            migrationBuilder.DeleteData(
                table: "VehicleModels",
                keyColumn: "Id",
                keyValue: 30);

            migrationBuilder.DeleteData(
                table: "VehicleBrands",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "VehicleBrands",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "VehicleBrands",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "VehicleBrands",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "VehicleBrands",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "VehicleBrands",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "VehicleBrands",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "VehicleBrands",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "VehicleBrands",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "VehicleBrands",
                keyColumn: "Id",
                keyValue: 10);
        }
    }
}
