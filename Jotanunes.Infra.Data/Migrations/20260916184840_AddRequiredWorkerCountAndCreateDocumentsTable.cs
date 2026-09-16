using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Jotanunes.Infra.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRequiredWorkerCountAndCreateDocumentsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RequiredWorkerCount",
                table: "company_work_sites",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "documents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    CompanyWorkSiteId = table.Column<long>(type: "bigint", nullable: true),
                    DocumentTypeId = table.Column<long>(type: "bigint", nullable: false),
                    UploadedBySupplierUserId = table.Column<long>(type: "bigint", nullable: false),
                    WorkerName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    WorkerCpf = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: true),
                    StorageKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    ReferencePeriodStart = table.Column<DateOnly>(type: "date", nullable: true),
                    ReferencePeriodEnd = table.Column<DateOnly>(type: "date", nullable: true),
                    ExpirationDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RejectionReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_documents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_documents_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_documents_company_work_sites_CompanyWorkSiteId",
                        column: x => x.CompanyWorkSiteId,
                        principalTable: "company_work_sites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_documents_document_types_DocumentTypeId",
                        column: x => x.DocumentTypeId,
                        principalTable: "document_types",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_documents_supplier_users_UploadedBySupplierUserId",
                        column: x => x.UploadedBySupplierUserId,
                        principalTable: "supplier_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_documents_CompanyId_DocumentTypeId_WorkerCpf_ReferencePerio~",
                table: "documents",
                columns: new[] { "CompanyId", "DocumentTypeId", "WorkerCpf", "ReferencePeriodStart" });

            migrationBuilder.CreateIndex(
                name: "IX_documents_CompanyWorkSiteId",
                table: "documents",
                column: "CompanyWorkSiteId");

            migrationBuilder.CreateIndex(
                name: "IX_documents_DocumentTypeId",
                table: "documents",
                column: "DocumentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_documents_UploadedBySupplierUserId",
                table: "documents",
                column: "UploadedBySupplierUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "documents");

            migrationBuilder.DropColumn(
                name: "RequiredWorkerCount",
                table: "company_work_sites");
        }
    }
}
