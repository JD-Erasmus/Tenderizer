using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tenderizer.Data.Migrations
{
    /// <inheritdoc />
    public partial class DocumentUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenderDocumentCvMetadata");

            migrationBuilder.DropColumn(
                name: "UploadedTenderDocumentId",
                table: "ChecklistItems");

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "LibraryDocuments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ChecklistDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChecklistItemId = table.Column<int>(type: "int", nullable: false),
                    StoredFileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LibraryDocumentVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DisplayName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    UploadedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UploadedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChecklistDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChecklistDocuments_ChecklistItems_ChecklistItemId",
                        column: x => x.ChecklistItemId,
                        principalTable: "ChecklistItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChecklistDocuments_LibraryDocumentVersions_LibraryDocumentVersionId",
                        column: x => x.LibraryDocumentVersionId,
                        principalTable: "LibraryDocumentVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChecklistDocuments_StoredFiles_StoredFileId",
                        column: x => x.StoredFileId,
                        principalTable: "StoredFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChecklistDocuments_Tenders_TenderId",
                        column: x => x.TenderId,
                        principalTable: "Tenders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LibraryDocuments_Type",
                table: "LibraryDocuments",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_ChecklistDocuments_ChecklistItemId",
                table: "ChecklistDocuments",
                column: "ChecklistItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ChecklistDocuments_LibraryDocumentVersionId",
                table: "ChecklistDocuments",
                column: "LibraryDocumentVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_ChecklistDocuments_StoredFileId",
                table: "ChecklistDocuments",
                column: "StoredFileId");

            migrationBuilder.CreateIndex(
                name: "IX_ChecklistDocuments_TenderId",
                table: "ChecklistDocuments",
                column: "TenderId");

            migrationBuilder.CreateIndex(
                name: "IX_ChecklistDocuments_TenderId_ChecklistItemId",
                table: "ChecklistDocuments",
                columns: new[] { "TenderId", "ChecklistItemId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChecklistDocuments");

            migrationBuilder.DropIndex(
                name: "IX_LibraryDocuments_Type",
                table: "LibraryDocuments");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "LibraryDocuments");

            migrationBuilder.AddColumn<Guid>(
                name: "UploadedTenderDocumentId",
                table: "ChecklistItems",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TenderDocumentCvMetadata",
                columns: table => new
                {
                    TenderDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsLeadConsultant = table.Column<bool>(type: "bit", nullable: false),
                    PersonName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ProjectRole = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenderDocumentCvMetadata", x => x.TenderDocumentId);
                    table.ForeignKey(
                        name: "FK_TenderDocumentCvMetadata_TenderDocuments_TenderDocumentId",
                        column: x => x.TenderDocumentId,
                        principalTable: "TenderDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }
    }
}
