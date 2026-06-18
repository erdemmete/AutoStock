using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoStock.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class VehicleQrStatusAndGenerationLimits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VehicleQrCodes_Vehicles_VehicleId",
                table: "VehicleQrCodes");

            migrationBuilder.DropIndex(
                name: "IX_VehicleQrCodes_VehicleId",
                table: "VehicleQrCodes");

            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT [Code]
                    FROM [VehicleQrCodes]
                    GROUP BY [Code]
                    HAVING COUNT(*) > 1
                )
                BEGIN
                    THROW 51000, 'VehicleQrCodes migration durduruldu: Aynı Code değerine sahip birden fazla QR kaydı var. Lütfen duplicate QR kodlarını raporlayıp düzeltin.', 1;
                END

                IF EXISTS (
                    SELECT 1
                    FROM [VehicleQrCodes]
                    WHERE LEN([Code]) > 64
                )
                BEGIN
                    THROW 51001, 'VehicleQrCodes migration durduruldu: 64 karakterden uzun QR Code değeri var. Lütfen ilgili kayıtları raporlayıp düzeltin.', 1;
                END

                IF EXISTS (
                    SELECT 1
                    FROM [VehicleQrCodes]
                    WHERE [IsAssigned] = 1
                      AND ([WorkshopId] IS NULL OR [VehicleId] IS NULL OR [AssignedAt] IS NULL)
                )
                BEGIN
                    THROW 51002, 'VehicleQrCodes migration durduruldu: Assigned görünen ama WorkshopId, VehicleId veya AssignedAt alanı eksik QR kaydı var.', 1;
                END

                IF EXISTS (
                    SELECT 1
                    FROM [VehicleQrCodes]
                    WHERE [IsAssigned] = 0
                      AND ([VehicleId] IS NOT NULL OR [AssignedAt] IS NOT NULL)
                )
                BEGIN
                    THROW 51003, 'VehicleQrCodes migration durduruldu: Atanmamış görünen ama VehicleId veya AssignedAt değeri dolu QR kaydı var.', 1;
                END

                IF EXISTS (
                    SELECT [VehicleId]
                    FROM [VehicleQrCodes]
                    WHERE [IsAssigned] = 1 AND [VehicleId] IS NOT NULL
                    GROUP BY [VehicleId]
                    HAVING COUNT(*) > 1
                )
                BEGIN
                    THROW 51004, 'VehicleQrCodes migration durduruldu: Aynı araçta birden fazla aktif QR kaydı var.', 1;
                END

                IF EXISTS (
                    SELECT 1
                    FROM [VehicleQrCodes] q
                    WHERE q.[WorkshopId] IS NOT NULL
                      AND NOT EXISTS (SELECT 1 FROM [Workshops] w WHERE w.[Id] = q.[WorkshopId])
                )
                BEGIN
                    THROW 51005, 'VehicleQrCodes migration durduruldu: Var olmayan WorkshopId içeren QR kaydı var.', 1;
                END

                IF EXISTS (
                    SELECT 1
                    FROM [VehicleQrCodes] q
                    WHERE q.[VehicleId] IS NOT NULL
                      AND NOT EXISTS (SELECT 1 FROM [Vehicles] v WHERE v.[Id] = q.[VehicleId])
                )
                BEGIN
                    THROW 51006, 'VehicleQrCodes migration durduruldu: Var olmayan VehicleId içeren QR kaydı var.', 1;
                END
                """);

            migrationBuilder.AddColumn<bool>(
                name: "QrGenerationEnabled",
                table: "Workshops",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "QrGenerationLimit",
                table: "Workshops",
                type: "int",
                nullable: false,
                defaultValue: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "VehicleQrCodes",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<int>(
                name: "AssignedByUserId",
                table: "VehicleQrCodes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedByUserId",
                table: "VehicleQrCodes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RetiredAt",
                table: "VehicleQrCodes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RetiredByUserId",
                table: "VehicleQrCodes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "VehicleQrCodes",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.Sql("""
                UPDATE [VehicleQrCodes]
                SET [Status] = CASE
                    WHEN [IsAssigned] = 1 THEN 2
                    ELSE 1
                END;
                """);

            migrationBuilder.DropColumn(
                name: "IsAssigned",
                table: "VehicleQrCodes");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleQrCodes_Code",
                table: "VehicleQrCodes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VehicleQrCodes_VehicleId",
                table: "VehicleQrCodes",
                column: "VehicleId",
                unique: true,
                filter: "[Status] = 2 AND [VehicleId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleQrCodes_WorkshopId",
                table: "VehicleQrCodes",
                column: "WorkshopId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_VehicleQrCodes_Assigned_State",
                table: "VehicleQrCodes",
                sql: "([Status] <> 2 OR ([WorkshopId] IS NOT NULL AND [VehicleId] IS NOT NULL AND [AssignedAt] IS NOT NULL))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_VehicleQrCodes_Available_State",
                table: "VehicleQrCodes",
                sql: "([Status] <> 1 OR ([VehicleId] IS NULL AND [AssignedAt] IS NULL AND [RetiredAt] IS NULL))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_VehicleQrCodes_Retired_State",
                table: "VehicleQrCodes",
                sql: "([Status] <> 3 OR [RetiredAt] IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_VehicleQrCodes_Status_Valid",
                table: "VehicleQrCodes",
                sql: "[Status] IN (1, 2, 3, 4)");

            migrationBuilder.AddForeignKey(
                name: "FK_VehicleQrCodes_Vehicles_VehicleId",
                table: "VehicleQrCodes",
                column: "VehicleId",
                principalTable: "Vehicles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_VehicleQrCodes_Workshops_WorkshopId",
                table: "VehicleQrCodes",
                column: "WorkshopId",
                principalTable: "Workshops",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VehicleQrCodes_Vehicles_VehicleId",
                table: "VehicleQrCodes");

            migrationBuilder.DropForeignKey(
                name: "FK_VehicleQrCodes_Workshops_WorkshopId",
                table: "VehicleQrCodes");

            migrationBuilder.DropIndex(
                name: "IX_VehicleQrCodes_Code",
                table: "VehicleQrCodes");

            migrationBuilder.DropIndex(
                name: "IX_VehicleQrCodes_VehicleId",
                table: "VehicleQrCodes");

            migrationBuilder.DropIndex(
                name: "IX_VehicleQrCodes_WorkshopId",
                table: "VehicleQrCodes");

            migrationBuilder.DropCheckConstraint(
                name: "CK_VehicleQrCodes_Assigned_State",
                table: "VehicleQrCodes");

            migrationBuilder.DropCheckConstraint(
                name: "CK_VehicleQrCodes_Available_State",
                table: "VehicleQrCodes");

            migrationBuilder.DropCheckConstraint(
                name: "CK_VehicleQrCodes_Retired_State",
                table: "VehicleQrCodes");

            migrationBuilder.DropCheckConstraint(
                name: "CK_VehicleQrCodes_Status_Valid",
                table: "VehicleQrCodes");

            migrationBuilder.DropColumn(
                name: "QrGenerationEnabled",
                table: "Workshops");

            migrationBuilder.DropColumn(
                name: "QrGenerationLimit",
                table: "Workshops");

            migrationBuilder.DropColumn(
                name: "AssignedByUserId",
                table: "VehicleQrCodes");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "VehicleQrCodes");

            migrationBuilder.DropColumn(
                name: "RetiredAt",
                table: "VehicleQrCodes");

            migrationBuilder.DropColumn(
                name: "RetiredByUserId",
                table: "VehicleQrCodes");

            migrationBuilder.AddColumn<bool>(
                name: "IsAssigned",
                table: "VehicleQrCodes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("""
                UPDATE [VehicleQrCodes]
                SET [IsAssigned] = CASE WHEN [Status] = 2 THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END;
                """);

            migrationBuilder.DropColumn(
                name: "Status",
                table: "VehicleQrCodes");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "VehicleQrCodes",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64);

            migrationBuilder.CreateIndex(
                name: "IX_VehicleQrCodes_VehicleId",
                table: "VehicleQrCodes",
                column: "VehicleId");

            migrationBuilder.AddForeignKey(
                name: "FK_VehicleQrCodes_Vehicles_VehicleId",
                table: "VehicleQrCodes",
                column: "VehicleId",
                principalTable: "Vehicles",
                principalColumn: "Id");
        }
    }
}
