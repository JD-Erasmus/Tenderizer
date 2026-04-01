using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tenderizer.Data.Migrations
{
    /// <inheritdoc />
    public partial class documentmanagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LibraryDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibraryDocuments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StoredFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StorageProvider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RelativePath = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    StoredFileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    LengthBytes = table.Column<long>(type: "bigint", nullable: false),
                    Sha256 = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    UploadedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UploadedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoredFiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LibraryDocumentVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LibraryDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StoredFileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    IsCurrent = table.Column<bool>(type: "bit", nullable: false),
                    ExpiryDateUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibraryDocumentVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LibraryDocumentVersions_LibraryDocuments_LibraryDocumentId",
                        column: x => x.LibraryDocumentId,
                        principalTable: "LibraryDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LibraryDocumentVersions_StoredFiles_StoredFileId",
                        column: x => x.StoredFileId,
                        principalTable: "StoredFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TenderDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StoredFileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LibraryDocumentVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Category = table.Column<int>(type: "int", nullable: false),
                    SourceType = table.Column<int>(type: "int", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    AttachedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AttachedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenderDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenderDocuments_LibraryDocumentVersions_LibraryDocumentVersionId",
                        column: x => x.LibraryDocumentVersionId,
                        principalTable: "LibraryDocumentVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TenderDocuments_StoredFiles_StoredFileId",
                        column: x => x.StoredFileId,
                        principalTable: "StoredFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TenderDocuments_Tenders_TenderId",
                        column: x => x.TenderId,
                        principalTable: "Tenders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TenderDocumentCvMetadata",
                columns: table => new
                {
                    TenderDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PersonName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ProjectRole = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsLeadConsultant = table.Column<bool>(type: "bit", nullable: false)
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

            migrationBuilder.CreateIndex(
                name: "IX_LibraryDocuments_Name",
                table: "LibraryDocuments",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LibraryDocumentVersions_LibraryDocumentId_IsCurrent",
                table: "LibraryDocumentVersions",
                columns: new[] { "LibraryDocumentId", "IsCurrent" },
                unique: true,
                filter: "[IsCurrent] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_LibraryDocumentVersions_LibraryDocumentId_VersionNumber",
                table: "LibraryDocumentVersions",
                columns: new[] { "LibraryDocumentId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LibraryDocumentVersions_StoredFileId",
                table: "LibraryDocumentVersions",
                column: "StoredFileId");

            migrationBuilder.CreateIndex(
                name: "IX_StoredFiles_RelativePath",
                table: "StoredFiles",
                column: "RelativePath",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenderDocuments_LibraryDocumentVersionId",
                table: "TenderDocuments",
                column: "LibraryDocumentVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_TenderDocuments_StoredFileId",
                table: "TenderDocuments",
                column: "StoredFileId");

            migrationBuilder.CreateIndex(
                name: "IX_TenderDocuments_TenderId_Category_AttachedAtUtc",
                table: "TenderDocuments",
                columns: new[] { "TenderId", "Category", "AttachedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenderDocumentCvMetadata");

            migrationBuilder.DropTable(
                name: "TenderDocuments");

            migrationBuilder.DropTable(
                name: "LibraryDocumentVersions");

            migrationBuilder.DropTable(
                name: "LibraryDocuments");

            migrationBuilder.DropTable(
                name: "StoredFiles");
        }
    }
}
