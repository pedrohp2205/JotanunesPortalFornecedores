using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Jotanunes.Infra.Data.Migrations
{
    /// <inheritdoc />
    public partial class CreateDocumentTypesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "document_types",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    AppliesTo = table.Column<int>(type: "integer", nullable: false),
                    Subject = table.Column<int>(type: "integer", nullable: false),
                    RequiresExpirationDate = table.Column<bool>(type: "boolean", nullable: false),
                    IsConditional = table.Column<bool>(type: "boolean", nullable: false),
                    ConditionDescription = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_types", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_document_types_Code",
                table: "document_types",
                column: "Code",
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

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

            migrationBuilder.Sql(
                "SELECT setval(pg_get_serial_sequence('document_types', 'Id'), (SELECT MAX(\"Id\") FROM document_types));");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "document_types");
        }
    }
}
