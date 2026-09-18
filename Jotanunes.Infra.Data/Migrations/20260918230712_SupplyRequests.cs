using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jotanunes.Infra.Data.Migrations
{
    /// <inheritdoc />
    // Transforma company_work_sites em supply_requests preservando os vínculos existentes
    // e os documentos que apontam para eles (não há recriação de tabela).
    public partial class SupplyRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_documents_company_work_sites_CompanyWorkSiteId",
                table: "documents");

            migrationBuilder.DropForeignKey(
                name: "FK_company_work_sites_companies_CompanyId",
                table: "company_work_sites");

            migrationBuilder.DropForeignKey(
                name: "FK_company_work_sites_work_sites_WorkSiteId",
                table: "company_work_sites");

            migrationBuilder.DropPrimaryKey(
                name: "PK_company_work_sites",
                table: "company_work_sites");

            migrationBuilder.DropIndex(
                name: "IX_company_work_sites_CompanyId_WorkSiteId",
                table: "company_work_sites");

            migrationBuilder.DropIndex(
                name: "IX_company_work_sites_WorkSiteId",
                table: "company_work_sites");

            migrationBuilder.RenameTable(
                name: "company_work_sites",
                newName: "supply_requests");

            migrationBuilder.RenameColumn(
                name: "CompanyWorkSiteId",
                table: "documents",
                newName: "SupplyRequestId");

            migrationBuilder.RenameIndex(
                name: "IX_documents_CompanyWorkSiteId",
                table: "documents",
                newName: "IX_documents_SupplyRequestId");

            migrationBuilder.AddColumn<int>(
                name: "SupplierType",
                table: "supply_requests",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "supply_requests",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<DateTime>(
                name: "ClosedAt",
                table: "supply_requests",
                type: "datetime2",
                nullable: true);

            // Até aqui toda empresa tinha um único tipo (Material ou ManpowerLabor);
            // o vínculo herda esse tipo.
            migrationBuilder.Sql(@"
                UPDATE sr
                SET sr.SupplierType = c.SupplierType
                FROM supply_requests sr
                INNER JOIN companies c ON c.Id = sr.CompanyId;");

            // Vínculos que já tinham sido desfeitos (exclusão lógica) viram solicitações canceladas (4).
            migrationBuilder.Sql(@"
                UPDATE supply_requests
                SET Status = 4, ClosedAt = DeletedAt
                WHERE DeletedAt IS NOT NULL;");

            // Remove os defaults temporários usados só para preencher as linhas existentes.
            migrationBuilder.AlterColumn<int>(
                name: "SupplierType",
                table: "supply_requests",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "supply_requests",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 1);

            migrationBuilder.AddPrimaryKey(
                name: "PK_supply_requests",
                table: "supply_requests",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_supply_requests_CompanyId_WorkSiteId_SupplierType",
                table: "supply_requests",
                columns: new[] { "CompanyId", "WorkSiteId", "SupplierType" },
                unique: true,
                filter: "[DeletedAt] IS NULL AND [Status] IN (1, 2)");

            migrationBuilder.CreateIndex(
                name: "IX_supply_requests_Status",
                table: "supply_requests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_supply_requests_WorkSiteId",
                table: "supply_requests",
                column: "WorkSiteId");

            migrationBuilder.AddForeignKey(
                name: "FK_supply_requests_companies_CompanyId",
                table: "supply_requests",
                column: "CompanyId",
                principalTable: "companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_supply_requests_work_sites_WorkSiteId",
                table: "supply_requests",
                column: "WorkSiteId",
                principalTable: "work_sites",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_documents_supply_requests_SupplyRequestId",
                table: "documents",
                column: "SupplyRequestId",
                principalTable: "supply_requests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        // Atenção: o índice único antigo é por (empresa, obra). Se já existirem duas solicitações
        // da mesma empresa na mesma obra (uma de cada tipo), o Down falha ao recriá-lo.
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_documents_supply_requests_SupplyRequestId",
                table: "documents");

            migrationBuilder.DropForeignKey(
                name: "FK_supply_requests_companies_CompanyId",
                table: "supply_requests");

            migrationBuilder.DropForeignKey(
                name: "FK_supply_requests_work_sites_WorkSiteId",
                table: "supply_requests");

            migrationBuilder.DropPrimaryKey(
                name: "PK_supply_requests",
                table: "supply_requests");

            migrationBuilder.DropIndex(
                name: "IX_supply_requests_CompanyId_WorkSiteId_SupplierType",
                table: "supply_requests");

            migrationBuilder.DropIndex(
                name: "IX_supply_requests_Status",
                table: "supply_requests");

            migrationBuilder.DropIndex(
                name: "IX_supply_requests_WorkSiteId",
                table: "supply_requests");

            migrationBuilder.DropColumn(
                name: "SupplierType",
                table: "supply_requests");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "supply_requests");

            migrationBuilder.DropColumn(
                name: "ClosedAt",
                table: "supply_requests");

            migrationBuilder.RenameTable(
                name: "supply_requests",
                newName: "company_work_sites");

            migrationBuilder.RenameColumn(
                name: "SupplyRequestId",
                table: "documents",
                newName: "CompanyWorkSiteId");

            migrationBuilder.RenameIndex(
                name: "IX_documents_SupplyRequestId",
                table: "documents",
                newName: "IX_documents_CompanyWorkSiteId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_company_work_sites",
                table: "company_work_sites",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_company_work_sites_CompanyId_WorkSiteId",
                table: "company_work_sites",
                columns: new[] { "CompanyId", "WorkSiteId" },
                unique: true,
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_company_work_sites_WorkSiteId",
                table: "company_work_sites",
                column: "WorkSiteId");

            migrationBuilder.AddForeignKey(
                name: "FK_company_work_sites_companies_CompanyId",
                table: "company_work_sites",
                column: "CompanyId",
                principalTable: "companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_company_work_sites_work_sites_WorkSiteId",
                table: "company_work_sites",
                column: "WorkSiteId",
                principalTable: "work_sites",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_documents_company_work_sites_CompanyWorkSiteId",
                table: "documents",
                column: "CompanyWorkSiteId",
                principalTable: "company_work_sites",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
