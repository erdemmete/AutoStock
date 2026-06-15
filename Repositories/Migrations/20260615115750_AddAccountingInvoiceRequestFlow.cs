using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoStock.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountingInvoiceRequestFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccountingInvoiceRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkshopId = table.Column<int>(type: "int", nullable: false),
                    InvoiceId = table.Column<int>(type: "int", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    AccountantEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountingInvoiceRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountingInvoiceRequests_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccountingInvoiceRequests_Workshops_WorkshopId",
                        column: x => x.WorkshopId,
                        principalTable: "Workshops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkshopEmailRecipients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkshopId = table.Column<int>(type: "int", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    RecipientType = table.Column<int>(type: "int", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkshopEmailRecipients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkshopEmailRecipients_Workshops_WorkshopId",
                        column: x => x.WorkshopId,
                        principalTable: "Workshops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OfficialInvoiceDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkshopId = table.Column<int>(type: "int", nullable: false),
                    InvoiceId = table.Column<int>(type: "int", nullable: false),
                    AccountingInvoiceRequestId = table.Column<int>(type: "int", nullable: true),
                    OfficialInvoiceNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OfficialInvoiceDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EttnOrUuid = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OriginalFileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    StoredFileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    RelativePath = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UploadedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfficialInvoiceDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OfficialInvoiceDocuments_AccountingInvoiceRequests_AccountingInvoiceRequestId",
                        column: x => x.AccountingInvoiceRequestId,
                        principalTable: "AccountingInvoiceRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_OfficialInvoiceDocuments_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfficialInvoiceDocuments_Workshops_WorkshopId",
                        column: x => x.WorkshopId,
                        principalTable: "Workshops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountingInvoiceRequests_InvoiceId",
                table: "AccountingInvoiceRequests",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountingInvoiceRequests_Token",
                table: "AccountingInvoiceRequests",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountingInvoiceRequests_WorkshopId_AccountantEmail_SentAt",
                table: "AccountingInvoiceRequests",
                columns: new[] { "WorkshopId", "AccountantEmail", "SentAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AccountingInvoiceRequests_WorkshopId_InvoiceId_Status",
                table: "AccountingInvoiceRequests",
                columns: new[] { "WorkshopId", "InvoiceId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_OfficialInvoiceDocuments_AccountingInvoiceRequestId",
                table: "OfficialInvoiceDocuments",
                column: "AccountingInvoiceRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_OfficialInvoiceDocuments_InvoiceId",
                table: "OfficialInvoiceDocuments",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_OfficialInvoiceDocuments_WorkshopId_InvoiceId",
                table: "OfficialInvoiceDocuments",
                columns: new[] { "WorkshopId", "InvoiceId" });

            migrationBuilder.CreateIndex(
                name: "IX_OfficialInvoiceDocuments_WorkshopId_OfficialInvoiceNumber",
                table: "OfficialInvoiceDocuments",
                columns: new[] { "WorkshopId", "OfficialInvoiceNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkshopEmailRecipients_WorkshopId_RecipientType_Email",
                table: "WorkshopEmailRecipients",
                columns: new[] { "WorkshopId", "RecipientType", "Email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkshopEmailRecipients_WorkshopId_RecipientType_IsActive",
                table: "WorkshopEmailRecipients",
                columns: new[] { "WorkshopId", "RecipientType", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OfficialInvoiceDocuments");

            migrationBuilder.DropTable(
                name: "WorkshopEmailRecipients");

            migrationBuilder.DropTable(
                name: "AccountingInvoiceRequests");
        }
    }
}
