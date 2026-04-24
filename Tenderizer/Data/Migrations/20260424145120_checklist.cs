using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tenderizer.Data.Migrations
{
    /// <inheritdoc />
    public partial class checklist : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ChecklistGeneratedAt",
                table: "Tenders",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ChecklistItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Required = table.Column<bool>(type: "bit", nullable: false),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    UploadedTenderDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LockedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    LockedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockExpiresAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChecklistItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChecklistItems_Tenders_TenderId",
                        column: x => x.TenderId,
                        principalTable: "Tenders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TenderAssignments",
                columns: table => new
                {
                    TenderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AssignedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenderAssignments", x => new { x.TenderId, x.UserId });
                    table.ForeignKey(
                        name: "FK_TenderAssignments_Tenders_TenderId",
                        column: x => x.TenderId,
                        principalTable: "Tenders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChecklistItems_IsCompleted",
                table: "ChecklistItems",
                column: "IsCompleted");

            migrationBuilder.CreateIndex(
                name: "IX_ChecklistItems_LockedByUserId",
                table: "ChecklistItems",
                column: "LockedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ChecklistItems_TenderId",
                table: "ChecklistItems",
                column: "TenderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChecklistItems");

            migrationBuilder.DropTable(
                name: "TenderAssignments");

            migrationBuilder.DropColumn(
                name: "ChecklistGeneratedAt",
                table: "Tenders");
        }
    }
}
