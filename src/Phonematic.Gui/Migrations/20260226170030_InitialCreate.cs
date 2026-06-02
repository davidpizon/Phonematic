using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Phonematic.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProcessedFiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FilePath = table.Column<string>(type: "TEXT", nullable: false),
                    FileHash = table.Column<string>(type: "TEXT", nullable: false),
                    FileSizeBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    TranscriptionPath = table.Column<string>(type: "TEXT", nullable: false),
                    TranscribedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    WhisperModel = table.Column<string>(type: "TEXT", nullable: false),
                    AudioDurationSeconds = table.Column<double>(type: "REAL", nullable: false),
                    TranscriptionDurationSeconds = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessedFiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TranscriptionChunks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProcessedFileId = table.Column<int>(type: "INTEGER", nullable: false),
                    ChunkIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    Text = table.Column<string>(type: "TEXT", nullable: false),
                    Embedding = table.Column<byte[]>(type: "BLOB", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TranscriptionChunks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TranscriptionChunks_ProcessedFiles_ProcessedFileId",
                        column: x => x.ProcessedFileId,
                        principalTable: "ProcessedFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedFiles_FilePath",
                table: "ProcessedFiles",
                column: "FilePath");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedFiles_FilePath_FileHash",
                table: "ProcessedFiles",
                columns: new[] { "FilePath", "FileHash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TranscriptionChunks_ProcessedFileId",
                table: "TranscriptionChunks",
                column: "ProcessedFileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TranscriptionChunks");

            migrationBuilder.DropTable(
                name: "ProcessedFiles");
        }
    }
}
