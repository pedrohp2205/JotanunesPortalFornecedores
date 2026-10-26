using System.Text.Json;
using Jotanunes.Application.DTOs.Analysis;
using Jotanunes.Application.Services.Analyzers;
using Jotanunes.Domain.Enums;
using Jotanunes.Tests.Support;

namespace Jotanunes.Tests.Application.Services.Analyzers;

public class FgtsAnalyzersTests
{
    private static readonly DateOnly Today = new(2026, 8, 10);

    private static Jotanunes.Domain.Entities.Document CompanyDocument(string code) =>
        AnalysisTestData.RecurringDocument(code, code, DocumentSubject.Company);

    private const string DetailText = """
        Vencimento da Guia: Número da Guia: Qtd. Trabalhadores FGTS: 20/08/2026 0126080754149858-0 3 Empregador:11.222.333 Nome Empregador:CONSTRUTORA EXEMPLO LTDA
        Detalhe da Guia Emitida 2.421,15 Total da Guia (FGTS + Consignado): Relação de Trabalhadores Tomador: 01.084.667/0001-82
        07/2026 MARIA APARECIDA DOS SANTOS 52998224725101030220240101 529.982.247-25 101 20/08/2026 Mensal 2.062,01 164,96 0,00 0,00 0,00 164,96
        07/2026 JOAO PEREIRA LIMA 11144477735101111120240101 111.444.777-35 101 20/08/2026 Mensal 1.721,32 137,70 0,00 0,00 0,00 137,70
        07/2026 ANA PAULA SOUZA 39053344705101111120240101 390.533.447-05 101 20/08/2026 Mensal 2.062,01 164,96 0,00 0,00 0,00 164,96
        Página 1 de 2 Vencimento da Guia: Número da Guia: Qtd. Trabalhadores FGTS: 20/08/2026 0126080754149858-0 3
        07/2026 MARIA APARECIDA DOS SANTOS 52998224725101030220240101 529.982.247-25 101 20/08/2026 Mensal 2.062,01 164,96 0,00 0,00 0,00 164,96
        """;

    [Fact]
    public void Should_Read_Fgts_Detail_With_One_Row_Per_Worker()
    {
        var extraction = new FgtsDetailAnalyzer().Extract(new DocumentText([DetailText]));

        Assert.True(extraction.IsComplete, string.Join(", ", extraction.Missing));
        Assert.Equal("11222333", extraction.Get("employerCnpjRoot"));
        Assert.Equal("07/2026", extraction.Get("competence"));
        Assert.Equal("20/08/2026", extraction.Get("dueDate"));
        Assert.Equal("3", extraction.Get("declaredWorkers"));
        Assert.Equal("2421.15", extraction.Get("guideTotal"));

        var workers = extraction.GetList<FgtsWorker>("workers");
        Assert.Equal(3, workers.Count);
        Assert.Equal(new FgtsWorker("MARIA APARECIDA DOS SANTOS", "52998224725", "2062.01", "164.96"), workers[0]);
    }

    [Fact]
    public void Should_Accept_Fgts_Detail_Of_The_Company_And_Period()
    {
        var analyzer = new FgtsDetailAnalyzer();

        Assert.Empty(analyzer.Validate(analyzer.Extract(new DocumentText([DetailText])), CompanyDocument("FGTS_DETAIL"), Today));
    }

    [Fact]
    public void Should_Flag_Fgts_Detail_From_Another_Company_Period_Or_Count()
    {
        var analyzer = new FgtsDetailAnalyzer();
        var text = DetailText.Replace("Empregador:11.222.333", "Empregador:11.444.777").Replace("07/2026 ", "06/2026 ").Replace(" 0126080754149858-0 3", " 0126080754149858-0 5");

        var findings = analyzer.Validate(analyzer.Extract(new DocumentText([text])), CompanyDocument("FGTS_DETAIL"), Today);

        Assert.Contains(findings, f => f.Code == "CNPJ_MISMATCH" && f.Severity == FindingSeverity.Blocking);
        Assert.Contains(findings, f => f.Code == "COMPETENCE_OUT_OF_PERIOD" && f.Severity == FindingSeverity.Blocking);
        Assert.Contains(findings, f => f.Code == "WORKER_COUNT_MISMATCH" && f.Severity == FindingSeverity.Warning);
    }

    [Fact]
    public void Should_Read_Fgts_Detail_Workers_From_Vision()
    {
        using var json = JsonDocument.Parse("""
            {"isExpectedDocument":true,"detectedDocument":"detalhe da guia","employerCnpjRoot":"11.222.333","competence":"07/2026","dueDate":"20/08/2026",
             "guideTotal":2421.15,"declaredWorkers":2,"workers":[
               {"name":"MARIA APARECIDA DOS SANTOS","cpf":"529.982.247-25","baseAmount":2062.01,"fgtsAmount":164.96},
               {"name":"ILEGIVEL","cpf":"123.456.789-00","baseAmount":null,"fgtsAmount":null}]}
            """);

        var extraction = new FgtsDetailAnalyzer().FromVision(json.RootElement);

        Assert.Equal("11222333", extraction.Get("employerCnpjRoot"));
        Assert.Single(extraction.GetList<FgtsWorker>("workers"));
    }

    [Fact]
    public void Should_Read_Fgts_Payment_Proof()
    {
        var analyzer = new FgtsPaymentProofAnalyzer();
        var extraction = analyzer.Extract(new DocumentText(["""
            comprovante de pagamento QR Code nome do pagador CONSTRUTORA EXEMPLO LTDA CPF/CNPJ do pagador 11.222.333/0001-81
            nome do recebedor CEF MATRIZ CPF/CNPJ do recebedor 00.360.305/0001-04 valor do documento R$ 2.421,15
            juros R$ 0,00 valor da transação R$ 2.421,15 pagamento efetuado em 08/08/2026 às 14:17:15
            """]));

        Assert.True(extraction.IsComplete, string.Join(", ", extraction.Missing));
        Assert.Equal("2421.15", extraction.Get("amount"));
        Assert.Equal("08/08/2026", extraction.Get("paymentDate"));
        Assert.Empty(analyzer.Validate(extraction, CompanyDocument("FGTS_PAYMENT_PROOF"), Today));
    }

    [Fact]
    public void Should_Warn_When_Fgts_Is_Paid_By_Another_Company_Or_Not_To_Caixa()
    {
        var analyzer = new FgtsPaymentProofAnalyzer();
        var extraction = analyzer.Extract(new DocumentText(["""
            comprovante de pagamento CPF/CNPJ do pagador 11.444.777/0001-61 CPF/CNPJ do recebedor 11.222.333/0001-81
            valor da transação R$ 2.421,15 pagamento efetuado em 08/08/2026
            """]));

        var findings = analyzer.Validate(extraction, CompanyDocument("FGTS_PAYMENT_PROOF"), Today);

        Assert.Contains(findings, f => f.Code == "PAYER_MISMATCH");
        Assert.Contains(findings, f => f.Code == "RECEIVER_NOT_CAIXA");
    }

    [Fact]
    public void Should_Read_Fgts_Guide_Only_By_Vision()
    {
        var analyzer = new FgtsGuideAnalyzer();
        using var json = JsonDocument.Parse("""
            {"isExpectedDocument":true,"detectedDocument":"GFD","employerCnpjRoot":"11.222.333","competence":"07/2026","declaredWorkers":3,
             "guideTotal":2421.15,"dueDate":"20/08/2026","guideIdentifier":"0126080754149858-0"}
            """);

        Assert.False(analyzer.Extract(new DocumentText(["qualquer texto"])).IsComplete);

        var extraction = analyzer.FromVision(json.RootElement);
        Assert.True(extraction.IsComplete, string.Join(", ", extraction.Missing));
        Assert.Equal("11222333", extraction.Get("employerCnpjRoot"));
        Assert.Equal("3", extraction.Get("declaredWorkers"));
        Assert.Empty(analyzer.Validate(extraction, CompanyDocument("FGTS_REPORT"), Today));
    }

    [Fact]
    public void Should_Read_Fgts_Guide_From_Ocr_Text()
    {
        var analyzer = new FgtsGuideAnalyzer();
        var extraction = analyzer.Extract(new DocumentText(["""
            Digital
            Pagar este documento até
            CPF/CNPJ do Empregador Nome/ Razão Social do Empregador 20 / 0) 8 / 20206
            11.222.333 | | CONSTRUTORA EXEMPLO LTDA a
            Valor a recolher
            Núm. de Pág. Identificador Tag 2 421 15
            1 0126080754149858-0 | | 11222333 07/2026 MENSAL ' '
            Informações de recolhimentos do FGTS
            Competência Trabalhadores FGTS Mensal FGTS Rescisório Compensatória Encargos FGTS Total
            07/2026 3 2.253,85 0,00 0,00 0,00 2253,85
            Total da Guia: 2.421,15
            """]));

        Assert.True(extraction.IsComplete, string.Join(", ", extraction.Missing));
        Assert.Equal("11222333", extraction.Get("employerCnpjRoot"));
        Assert.Equal("07/2026", extraction.Get("competence"));
        Assert.Equal("3", extraction.Get("declaredWorkers"));
        Assert.Equal("2421.15", extraction.Get("guideTotal"));
        Assert.Equal("0126080754149858-0", extraction.Get("guideIdentifier"));
        Assert.Null(extraction.Get("dueDate"));
        Assert.Empty(analyzer.Validate(extraction, CompanyDocument("FGTS_REPORT"), Today));
    }

    [Fact]
    public void Should_Read_Readable_Due_Date_From_Ocr_Text()
    {
        var extraction = new FgtsGuideAnalyzer().Extract(new DocumentText([
            "Guia do FGTS Digital Pagar este documento até 20/08/2026 Empregador 11.222.333 07/2026 3 2.253,85 Total da Guia: 2.421,15"]));

        Assert.Equal("20/08/2026", extraction.Get("dueDate"));
    }
}
