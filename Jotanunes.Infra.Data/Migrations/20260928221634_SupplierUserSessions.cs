using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jotanunes.Infra.Data.Migrations
{
    /// <inheritdoc />
    public partial class SupplierUserSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_supplier_users_RefreshToken",
                table: "supplier_users");

            migrationBuilder.DropColumn(
                name: "RefreshToken",
                table: "supplier_users");

            migrationBuilder.DropColumn(
                name: "RefreshTokenExpiresAt",
                table: "supplier_users");

            migrationBuilder.AddColumn<string>(
                name: "SecurityStamp",
                table: "supplier_users",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("UPDATE [supplier_users] SET [SecurityStamp] = LOWER(REPLACE(CONVERT(varchar(36), NEWID()), '-', ''))");

            migrationBuilder.CreateTable(
                name: "supplier_user_sessions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SupplierUserId = table.Column<long>(type: "bigint", nullable: false),
                    RefreshTokenHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PreviousRefreshTokenHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    RotatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_supplier_user_sessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_supplier_user_sessions_supplier_users_SupplierUserId",
                        column: x => x.SupplierUserId,
                        principalTable: "supplier_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_supplier_user_sessions_PreviousRefreshTokenHash",
                table: "supplier_user_sessions",
                column: "PreviousRefreshTokenHash");

            migrationBuilder.CreateIndex(
                name: "IX_supplier_user_sessions_RefreshTokenHash",
                table: "supplier_user_sessions",
                column: "RefreshTokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_supplier_user_sessions_SupplierUserId",
                table: "supplier_user_sessions",
                column: "SupplierUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "supplier_user_sessions");

            migrationBuilder.DropColumn(
                name: "SecurityStamp",
                table: "supplier_users");

            migrationBuilder.AddColumn<string>(
                name: "RefreshToken",
                table: "supplier_users",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RefreshTokenExpiresAt",
                table: "supplier_users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_supplier_users_RefreshToken",
                table: "supplier_users",
                column: "RefreshToken");
        }
    }
}
