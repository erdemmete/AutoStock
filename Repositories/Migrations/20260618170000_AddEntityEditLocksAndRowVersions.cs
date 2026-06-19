using System;
using AutoStock.Repositories;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoStock.Repositories.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260618170000_AddEntityEditLocksAndRowVersions")]
    public partial class AddEntityEditLocksAndRowVersions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ServiceRecords",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: Array.Empty<byte>());

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Invoices",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: Array.Empty<byte>());

            migrationBuilder.CreateTable(
                name: "EntityEditLocks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkshopId = table.Column<int>(type: "int", nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    EntityId = table.Column<int>(type: "int", nullable: false),
                    LockedByUserId = table.Column<int>(type: "int", nullable: false),
                    LockToken = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    AcquiredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastHeartbeatAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntityEditLocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EntityEditLocks_AspNetUsers_LockedByUserId",
                        column: x => x.LockedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EntityEditLocks_Workshops_WorkshopId",
                        column: x => x.WorkshopId,
                        principalTable: "Workshops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EntityEditLocks_LockedByUserId_ExpiresAt",
                table: "EntityEditLocks",
                columns: new[] { "LockedByUserId", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_EntityEditLocks_WorkshopId_EntityType_EntityId",
                table: "EntityEditLocks",
                columns: new[] { "WorkshopId", "EntityType", "EntityId" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EntityEditLocks");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ServiceRecords");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Invoices");
        }
    }
}
