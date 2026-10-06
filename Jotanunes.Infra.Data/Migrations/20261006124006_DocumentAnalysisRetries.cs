using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jotanunes.Infra.Data.Migrations
{
    /// <inheritdoc />
    public partial class DocumentAnalysisRetries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "NextAttemptAt",
                table: "document_analyses",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NextAttemptAt",
                table: "document_analyses");
        }
    }
}
