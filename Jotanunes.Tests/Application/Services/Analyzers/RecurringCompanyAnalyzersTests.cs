using Jotanunes.Application.DTOs.Analysis;
using Jotanunes.Application.Services.Analyzers;
using Jotanunes.Domain.Enums;
using Jotanunes.Tests.Support;

namespace Jotanunes.Tests.Application.Services.Analyzers;

public class RecurringCompanyAnalyzersTests
{
    private static readonly DateOnly Today = new(2026, 8, 10);

    private static Jotanunes.Domain.Entities.Document CompanyDocument(string code) =>
        AnalysisTestData.RecurringDocument(code, code, DocumentSubject.Company);

    [Fact]
    public void Should_Read_Dctfweb_From_Ocr_Text()
    {
        var analyzer = new DctfWebAnalyzer();
        var extraction = analyzer.Extract(new DocumentText([
            $"Recibo de Entrega da Declaração de Débitos e Créditos Tributários Federais - DCTFWeb CNPJ/CPF|{AnalysisTestData.CompanyCnpj} Período de apuração|07/2026 TOTAL R$ 2.833,29 R$ 0,00",
            "DCTFWeb recebida via Internet Nº do recibo de entrega|0000050000513295660"]));

        Assert.True(extraction.IsComplete, string.Join(", ", extraction.Missing));
        Assert.Equal("07/2026", extraction.Get("competence"));
        Assert.Equal("2833.29", extraction.Get("totalDebits"));
        Assert.Equal("0000050000513295660", extraction.Get("receiptNumber"));
        Assert.Empty(analyzer.Validate(extraction, CompanyDocument("DCTFWEB_RECEIPT"), Today));
    }

    [Fact]
    public void Should_Block_Dctfweb_Of_Another_Period()
    {
        var analyzer = new DctfWebAnalyzer();
        var extraction = analyzer.Extract(new DocumentText([$"RELATÓRIO DA DECLARAÇÃO COMPLETA - DCTFWeb CNPJ/{AnalysisTestData.CompanyCnpj} Período apuração|05/2026"]));

        Assert.Contains(analyzer.Validate(extraction, CompanyDocument("DCTFWEB_REPORT"), Today), f => f.Code == "COMPETENCE_OUT_OF_PERIOD");
    }

    private const string PayrollText = """
        Folha de Pagamento Pag.: 1 de 2 Empresa: CONSTRUTORA EXEMPLO LTDA - CNPJ: 11.222.333/0001-81 Mês/Ano: 07/2026
        000016 MARIA APARECIDA DOS SANTOS Cargo: PORTEIRO 011 Salário-Base 30 dia(s) 1.721,32 FGTS: 164,96 Líquido a receber: 1.900,75 Data: / / Assinatura:
        000004 JOAO PEREIRA LIMA Cargo: SERVENTE 011 Salário-Base 30 dia(s) 1.721,32 Data: / / Assinatura:
        000009 —ANA PAULA SOUZA Cargo: PORTEIRO 011 Salário-Base 30 dia(s) 1.721,32 FGTS: 164,96 Líquido a receber: 2.035,83
        000029 CARLOS SILVA Cargo: SOCIO Total Geral (4 empregados) FGTS: 2.040,03 Líquido a receber: 27.952,81
        """;

    [Fact]
    public void Should_Read_One_Payroll_Block_Per_Employee_Without_Taking_Totals()
    {
        var extraction = new PayrollAnalyzer().Extract(new DocumentText([PayrollText]));

        Assert.True(extraction.IsComplete, string.Join(", ", extraction.Missing));
        Assert.Equal("07/2026", extraction.Get("competence"));
        Assert.Equal(
            [
                new PayrollEmployee("MARIA APARECIDA DOS SANTOS", "1900.75", "164.96"),
                new PayrollEmployee("JOAO PEREIRA LIMA", null, null),
                new PayrollEmployee("ANA PAULA SOUZA", "2035.83", "164.96"),
                new PayrollEmployee("CARLOS SILVA", null, null)
            ],
            extraction.GetList<PayrollEmployee>("employees"));
    }

    [Fact]
    public void Should_Not_Read_Payment_Receipt_As_Payroll()
    {
        var extraction = new PayrollAnalyzer().Extract(new DocumentText(["Recibo de Pagamento ( Folha de Pagamento ) Competência Julho de 2026"]));

        Assert.Contains("identificação como folha de pagamento", extraction.Missing);
    }
}
