using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Transcriptonator.Migrations
{
    /// <inheritdoc />
    public partial class AddPlaudRecordings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlaudRecordings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlaudFileId = table.Column<string>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    RecordedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DurationSeconds = table.Column<double>(type: "REAL", nullable: false),
                    FolderName = table.Column<string>(type: "TEXT", nullable: true),
                    LocalFilePath = table.Column<string>(type: "TEXT", nullable: true),
                    IsDownloaded = table.Column<bool>(type: "INTEGER", nullable: false),
                    DownloadedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FileSizeBytes = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlaudRecordings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlaudRecordings_PlaudFileId",
                table: "PlaudRecordings",
                column: "PlaudFileId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlaudRecordings");
        }
    }
}
