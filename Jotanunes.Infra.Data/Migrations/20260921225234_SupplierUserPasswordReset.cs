using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jotanunes.Infra.Data.Migrations
{
    /// <inheritdoc />
    public partial class SupplierUserPasswordReset : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PasswordResetExpiresAt",
                table: "supplier_users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PasswordResetTokenHash",
                table: "supplier_users",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PasswordResetExpiresAt",
                table: "supplier_users");

            migrationBuilder.DropColumn(
                name: "PasswordResetTokenHash",
                table: "supplier_users");
        }
    }
}
