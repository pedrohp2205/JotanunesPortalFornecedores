using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jotanunes.Infra.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentRequirements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "document_requirements",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SupplyRequestId = table.Column<long>(type: "bigint", nullable: false),
                    DocumentTypeId = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_requirements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_document_requirements_document_types_DocumentTypeId",
                        column: x => x.DocumentTypeId,
                        principalTable: "document_types",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_document_requirements_supply_requests_SupplyRequestId",
                        column: x => x.SupplyRequestId,
                        principalTable: "supply_requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_document_requirements_DocumentTypeId",
                table: "document_requirements",
                column: "DocumentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_document_requirements_SupplyRequestId_DocumentTypeId",
                table: "document_requirements",
                columns: new[] { "SupplyRequestId", "DocumentTypeId" },
                unique: true,
                filter: "[DeletedAt] IS NULL");

            // Preserva o comportamento anterior: cada solicitacao que ja existe passa a
            // exigir exatamente os tipos que a regra cobrava antes desta tabela existir.
            migrationBuilder.Sql(@"
                INSERT INTO [document_requirements] ([SupplyRequestId], [DocumentTypeId], [CreatedAt], [UpdatedAt])
                SELECT sr.[Id], dt.[Id], SYSUTCDATETIME(), SYSUTCDATETIME()
                FROM [supply_requests] sr
                INNER JOIN [document_types] dt
                    ON dt.[Active] = 1
                   AND dt.[DeletedAt] IS NULL
                   AND (dt.[AppliesTo] & sr.[SupplierType]) <> 0
                WHERE sr.[DeletedAt] IS NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "document_requirements");
        }
    }
}
