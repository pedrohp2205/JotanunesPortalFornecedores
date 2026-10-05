using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jotanunes.Infra.Data.Migrations
{
    /// <inheritdoc />
    public partial class Workers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "WorkerId",
                table: "documents",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "workers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Cpf = table.Column<string>(type: "nvarchar(11)", maxLength: 11, nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_workers_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "worker_allocations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SupplyRequestId = table.Column<long>(type: "bigint", nullable: false),
                    WorkerId = table.Column<long>(type: "bigint", nullable: false),
                    AllocatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReleasedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_worker_allocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_worker_allocations_supply_requests_SupplyRequestId",
                        column: x => x.SupplyRequestId,
                        principalTable: "supply_requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_worker_allocations_workers_WorkerId",
                        column: x => x.WorkerId,
                        principalTable: "workers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql(@"
INSERT INTO workers (CompanyId, Name, Cpf, Active, UpdatedAt, CreatedAt)
SELECT CompanyId, COALESCE(NULLIF(LTRIM(RTRIM(WorkerName)), ''), WorkerCpf), WorkerCpf, 1, SYSUTCDATETIME(), SYSUTCDATETIME()
FROM (
    SELECT CompanyId, WorkerCpf, WorkerName,
           ROW_NUMBER() OVER (PARTITION BY CompanyId, WorkerCpf ORDER BY CreatedAt DESC) AS rn
    FROM documents
    WHERE WorkerCpf IS NOT NULL
) latest
WHERE rn = 1;");

            migrationBuilder.Sql(@"
UPDATE d SET d.WorkerId = w.Id
FROM documents d
INNER JOIN workers w ON w.CompanyId = d.CompanyId AND w.Cpf = d.WorkerCpf
WHERE d.WorkerCpf IS NOT NULL;");

            migrationBuilder.Sql(@"
INSERT INTO worker_allocations (SupplyRequestId, WorkerId, AllocatedAt, ReleasedAt, UpdatedAt, CreatedAt)
SELECT d.SupplyRequestId, d.WorkerId, MIN(d.CreatedAt),
       CASE WHEN sr.Status IN (1, 2) AND sr.DeletedAt IS NULL THEN NULL ELSE COALESCE(sr.ClosedAt, SYSUTCDATETIME()) END,
       SYSUTCDATETIME(), SYSUTCDATETIME()
FROM documents d
INNER JOIN supply_requests sr ON sr.Id = d.SupplyRequestId
WHERE d.WorkerId IS NOT NULL AND d.SupplyRequestId IS NOT NULL
GROUP BY d.SupplyRequestId, d.WorkerId, sr.Status, sr.ClosedAt, sr.DeletedAt;");

            migrationBuilder.DropIndex(
                name: "IX_documents_CompanyId_DocumentTypeId_WorkerCpf_ReferencePeriodStart",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "WorkerCpf",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "WorkerName",
                table: "documents");

            migrationBuilder.CreateIndex(
                name: "IX_documents_CompanyId_DocumentTypeId_WorkerId_ReferencePeriodStart",
                table: "documents",
                columns: new[] { "CompanyId", "DocumentTypeId", "WorkerId", "ReferencePeriodStart" });

            migrationBuilder.CreateIndex(
                name: "IX_documents_WorkerId",
                table: "documents",
                column: "WorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_worker_allocations_SupplyRequestId_WorkerId",
                table: "worker_allocations",
                columns: new[] { "SupplyRequestId", "WorkerId" },
                unique: true,
                filter: "[DeletedAt] IS NULL AND [ReleasedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_worker_allocations_WorkerId",
                table: "worker_allocations",
                column: "WorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_workers_CompanyId_Cpf",
                table: "workers",
                columns: new[] { "CompanyId", "Cpf" },
                unique: true,
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_documents_workers_WorkerId",
                table: "documents",
                column: "WorkerId",
                principalTable: "workers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "WorkerCpf",
                table: "documents",
                type: "nvarchar(11)",
                maxLength: 11,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkerName",
                table: "documents",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.Sql(@"
UPDATE d SET d.WorkerName = w.Name, d.WorkerCpf = w.Cpf
FROM documents d
INNER JOIN workers w ON w.Id = d.WorkerId;");

            migrationBuilder.DropForeignKey(
                name: "FK_documents_workers_WorkerId",
                table: "documents");

            migrationBuilder.DropTable(
                name: "worker_allocations");

            migrationBuilder.DropTable(
                name: "workers");

            migrationBuilder.DropIndex(
                name: "IX_documents_CompanyId_DocumentTypeId_WorkerId_ReferencePeriodStart",
                table: "documents");

            migrationBuilder.DropIndex(
                name: "IX_documents_WorkerId",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "WorkerId",
                table: "documents");

            migrationBuilder.CreateIndex(
                name: "IX_documents_CompanyId_DocumentTypeId_WorkerCpf_ReferencePeriodStart",
                table: "documents",
                columns: new[] { "CompanyId", "DocumentTypeId", "WorkerCpf", "ReferencePeriodStart" });
        }
    }
}
