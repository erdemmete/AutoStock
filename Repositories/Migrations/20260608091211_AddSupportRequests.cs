using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoStock.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddSupportRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SupportRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkshopId = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    RequestType = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    Priority = table.Column<int>(type: "int", nullable: false, defaultValue: 2),
                    Subject = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    RequestedUserFullName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    RequestedUserPhone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    RequestedUserEmail = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    RequestedUserRole = table.Column<int>(type: "int", nullable: true),
                    AdminResponse = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    RespondedByUserId = table.Column<int>(type: "int", nullable: true),
                    RespondedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "CAST(SYSUTCDATETIME() AT TIME ZONE 'UTC' AT TIME ZONE 'Turkey Standard Time' AS datetime2)"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupportRequests_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupportRequests_AspNetUsers_RespondedByUserId",
                        column: x => x.RespondedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupportRequests_Workshops_WorkshopId",
                        column: x => x.WorkshopId,
                        principalTable: "Workshops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SupportRequests_CreatedAt",
                table: "SupportRequests",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SupportRequests_CreatedByUserId",
                table: "SupportRequests",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportRequests_RequestType",
                table: "SupportRequests",
                column: "RequestType");

            migrationBuilder.CreateIndex(
                name: "IX_SupportRequests_RespondedByUserId",
                table: "SupportRequests",
                column: "RespondedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportRequests_Status",
                table: "SupportRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SupportRequests_WorkshopId",
                table: "SupportRequests",
                column: "WorkshopId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportRequests_WorkshopId_Status_RequestType",
                table: "SupportRequests",
                columns: new[] { "WorkshopId", "Status", "RequestType" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SupportRequests");
        }
    }
}
