using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jotanunes.Infra.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "companies",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Cnpj = table.Column<string>(type: "nvarchar(14)", maxLength: 14, nullable: false),
                    CorporateName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TradeName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StateRegistration = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(11)", maxLength: 11, nullable: false),
                    ResponsibleName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SupplierType = table.Column<int>(type: "int", nullable: false),
                    AddressStreet = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AddressNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AddressComplement = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AddressNeighborhood = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AddressCity = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AddressState = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    AddressZipCode = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_companies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "document_types",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Category = table.Column<int>(type: "int", nullable: false),
                    AppliesTo = table.Column<int>(type: "int", nullable: false),
                    Subject = table.Column<int>(type: "int", nullable: false),
                    RequiresExpirationDate = table.Column<bool>(type: "bit", nullable: false),
                    IsConditional = table.Column<bool>(type: "bit", nullable: false),
                    ConditionDescription = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Active = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_types", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "work_sites",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    RenewalPeriodDays = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_work_sites", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "supplier_users",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false),
                    MustChangePassword = table.Column<bool>(type: "bit", nullable: false),
                    LastAccessAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RefreshToken = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    RefreshTokenExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FailedLoginAttempts = table.Column<int>(type: "int", nullable: false),
                    LockedUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_supplier_users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_supplier_users_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "company_work_sites",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    WorkSiteId = table.Column<long>(type: "bigint", nullable: false),
                    RequiredWorkerCount = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_company_work_sites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_company_work_sites_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_company_work_sites_work_sites_WorkSiteId",
                        column: x => x.WorkSiteId,
                        principalTable: "work_sites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "documents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    CompanyWorkSiteId = table.Column<long>(type: "bigint", nullable: true),
                    DocumentTypeId = table.Column<long>(type: "bigint", nullable: false),
                    UploadedBySupplierUserId = table.Column<long>(type: "bigint", nullable: false),
                    WorkerName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    WorkerCpf = table.Column<string>(type: "nvarchar(11)", maxLength: 11, nullable: true),
                    StorageKey = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ReferencePeriodStart = table.Column<DateOnly>(type: "date", nullable: true),
                    ReferencePeriodEnd = table.Column<DateOnly>(type: "date", nullable: true),
                    ExpirationDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RejectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
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
                name: "IX_companies_Cnpj",
                table: "companies",
                column: "Cnpj",
                unique: true,
                filter: "[DeletedAt] IS NULL");

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

            migrationBuilder.CreateIndex(
                name: "IX_document_types_Code",
                table: "document_types",
                column: "Code",
                unique: true,
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_documents_CompanyId_DocumentTypeId_WorkerCpf_ReferencePeriodStart",
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

            migrationBuilder.CreateIndex(
                name: "IX_supplier_users_CompanyId",
                table: "supplier_users",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_supplier_users_Email",
                table: "supplier_users",
                column: "Email",
                unique: true,
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_supplier_users_RefreshToken",
                table: "supplier_users",
                column: "RefreshToken");

            var seedDate = new DateTime(2026, 9, 16, 0, 0, 0, DateTimeKind.Utc);

            migrationBuilder.InsertData(
                table: "document_types",
                columns: new[] { "Id", "Code", "Name", "Category", "AppliesTo", "Subject", "RequiresExpirationDate", "IsConditional", "ConditionDescription", "Active", "CreatedAt", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "CNPJ_CARD", "Cartão de CNPJ", 1, 3, 1, false, false, null!, true, seedDate, seedDate },
                    { 2, "SOCIAL_CONTRACT", "Registro de Contrato Social", 1, 3, 1, false, false, null!, true, seedDate, seedDate },
                    { 3, "ADDRESS_PROOF_COMPANY", "Comprovante de Endereço Comercial", 1, 3, 1, false, false, null!, true, seedDate, seedDate },
                    { 4, "FGTS_CND", "Certidão Negativa de FGTS", 1, 3, 1, true, false, null!, true, seedDate, seedDate },
                    { 5, "FEDERAL_CND", "Certidão Negativa de Débito (INSS/Receita Federal)", 1, 3, 1, true, false, null!, true, seedDate, seedDate },
                    { 6, "SIMPLES_NACIONAL", "Comprovante do Simples Nacional", 1, 3, 1, false, true, "Aplicável quando o fornecedor é optante do Simples Nacional", true, seedDate, seedDate },
                    { 7, "MUNICIPAL_LICENSE", "Licença de Operação Municipal", 1, 3, 1, false, true, "Aplicável a empresas de transporte de resíduos sólidos", true, seedDate, seedDate },
                    { 8, "CGCRE_CERTIFICATION", "Certificação CGCRE/INMETRO", 1, 3, 1, false, true, "Aplicável a empresas de controle tecnológico de solo", true, seedDate, seedDate },
                    { 9, "ART", "Anotação de Responsabilidade Técnica (ART)", 1, 3, 1, false, true, "Aplicável a projetos, cobertura/telhamento, sondagem, controle tecnológico de solo, instalações de combate a incêndio/gás, laudo de aterramento, PGRCC/PCMAT e esquadrias de alumínio", true, seedDate, seedDate },
                    { 10, "PARTNER_ID", "RG/CPF dos Sócios", 1, 3, 1, false, false, null!, true, seedDate, seedDate },
                    { 11, "PAYROLL", "Folha de Pagamento", 2, 2, 1, false, false, null!, true, seedDate, seedDate },
                    { 12, "FGTS_PAYMENT_PROOF", "Comprovante de Pagamento do FGTS", 2, 2, 1, false, false, null!, true, seedDate, seedDate },
                    { 13, "FGTS_DETAIL", "Detalhamento do FGTS", 2, 2, 1, false, false, null!, true, seedDate, seedDate },
                    { 14, "FGTS_REPORT", "Relatório do FGTS", 2, 2, 1, false, false, null!, true, seedDate, seedDate },
                    { 15, "DCTFWEB_RECEIPT", "Recibo de Entrega do DCTFWeb", 2, 2, 1, false, false, null!, true, seedDate, seedDate },
                    { 16, "DCTFWEB_REPORT", "Relatório do DCTFWeb", 2, 2, 1, false, false, null!, true, seedDate, seedDate },
                    { 17, "EMPLOYEE_LIST", "Relação dos Funcionários Lotados na Obra", 2, 2, 1, false, false, null!, true, seedDate, seedDate },
                    { 18, "TIMESHEET", "Folha de Ponto", 2, 2, 2, false, false, null!, true, seedDate, seedDate },
                    { 19, "PAYMENT_PROOF", "Comprovante de Pagamento", 2, 2, 2, false, false, null!, true, seedDate, seedDate },
                    { 20, "PAYMENT_RECEIPT", "Recibo de Pagamento", 2, 2, 2, false, false, null!, true, seedDate, seedDate }
                });

            migrationBuilder.Sql("DBCC CHECKIDENT ('document_types', RESEED, 20);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "documents");

            migrationBuilder.DropTable(
                name: "company_work_sites");

            migrationBuilder.DropTable(
                name: "document_types");

            migrationBuilder.DropTable(
                name: "supplier_users");

            migrationBuilder.DropTable(
                name: "work_sites");

            migrationBuilder.DropTable(
                name: "companies");
        }
    }
}
