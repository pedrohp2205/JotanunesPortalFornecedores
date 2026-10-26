using System.Text.Json;
using Jotanunes.Application.DTOs.Analysis;
using Jotanunes.Application.Services.Analyzers;
using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Enums;
using Jotanunes.Tests.Support;

namespace Jotanunes.Tests.Application.Services.Analyzers;

public class PaymentProofAnalyzerTests
{
    private readonly PaymentProofAnalyzer _analyzer = new();

    private static string ProofText(
        string payeeCpf = AnalysisTestData.WorkerCpf,
        string payerCnpj = AnalysisTestData.CompanyCnpj,
        string date = "06/08/2026",
        string amount = "1.900,75")
    {
        return $"""
            Comprovante de transferência Dados de quem está pagando Nome CONSTRUTORA EXEMPLO LTDA CPF ou CNPJ {payerCnpj}
            Agência e conta 1007/22202-7 Dados de quem está recebendo Nome MARIA APARECIDA DOS SANTOS CPF ou CNPJ {payeeCpf}
            Instituição BANCO EXEMPLO Dados da transação Valor R$ {amount} Data da transferência {date}
            Tipo de Pagamento PIX - pagamento instantâneo
            """;
    }

    private List<AnalysisFinding> Validate(FieldExtraction extraction, Document? document = null)
    {
        Assert.True(extraction.IsComplete, string.Join(", ", extraction.Missing));
        return _analyzer.Validate(extraction, document ?? AnalysisTestData.RecurringDocument("PAYMENT_PROOF", "Comprovante de Pagamento", DocumentSubject.Worker), new DateOnly(2026, 8, 10)).ToList();
    }

    private FieldExtraction Read(string text) => _analyzer.Extract(new DocumentText([text]));

    [Fact]
    public void Should_Read_Payer_Payee_Amount_And_Date()
    {
        var extraction = Read(ProofText());

        Assert.True(extraction.IsComplete);
        Assert.Equal("11222333000181", extraction.Get("payerCnpj"));
        Assert.Equal("MARIA APARECIDA DOS SANTOS", extraction.Get("payeeName"));
        Assert.Equal("52998224725", extraction.Get("payeeCpf"));
        Assert.Equal("1900.75", extraction.Get("amount"));
        Assert.Equal("06/08/2026", extraction.Get("paymentDate"));
    }

    [Fact]
    public void Should_Keep_Partially_Hidden_Cpf()
    {
        Assert.Equal("***982247**", Read(ProofText(payeeCpf: "***.982.247-**")).Get("payeeCpf"));
    }

    [Fact]
    public void Should_Report_Nothing_For_Matching_Proof_Paid_On_Time()
    {
        Assert.Empty(Validate(Read(ProofText())));
    }

    [Fact]
    public void Should_Accept_Hidden_Cpf_When_Visible_Digits_Match()
    {
        var finding = Assert.Single(Validate(Read(ProofText(payeeCpf: "***.982.247-**"))));

        Assert.Equal("CPF_PARTIALLY_HIDDEN", finding.Code);
        Assert.Equal(FindingSeverity.Info, finding.Severity);
    }

    [Theory]
    [InlineData("***.111.222-**")]
    [InlineData("111.444.777-35")]
    public void Should_Block_Proof_Paid_To_Another_Person(string payeeCpf)
    {
        var findings = Validate(Read(ProofText(payeeCpf: payeeCpf)));

        Assert.Contains(findings, f => f.Code == "WORKER_CPF_MISMATCH" && f.Severity == FindingSeverity.Blocking);
    }

    [Fact]
    public void Should_Warn_When_Paid_By_Another_Cnpj()
    {
        var findings = Validate(Read(ProofText(payerCnpj: "11.444.777/0001-61")));

        Assert.Contains(findings, f => f.Code == "PAYER_MISMATCH" && f.Severity == FindingSeverity.Warning);
    }

    [Fact]
    public void Should_Warn_When_Salary_Is_Paid_After_The_Fifth_Business_Day()
    {
        var finding = Assert.Single(Validate(Read(ProofText(date: "07/08/2026"))));

        Assert.Equal("LATE_SALARY_PAYMENT", finding.Code);
        Assert.Contains("06/08/2026", finding.Message);
    }

    [Fact]
    public void Should_Not_Read_Fgts_Payment_As_Salary_Payment()
    {
        var extraction = Read("comprovante de pagamento QR Code dados do recebedor nome do recebedor CEF MATRIZ CPF/CNPJ do recebedor 00.360.305/0001-04 valor da transação R$ 2.421,15 pagamento efetuado em 08/08/2026");

        Assert.Contains("CPF de quem recebeu", extraction.Missing);
    }

    [Fact]
    public void Should_Map_Vision_Result_With_Hidden_Cpf()
    {
        using var json = JsonDocument.Parse("""
            {"isExpectedDocument":true,"detectedDocument":"comprovante PIX","payerName":"CONSTRUTORA EXEMPLO LTDA","payerDocument":"11.222.333/0001-81",
             "payeeName":"MARIA APARECIDA DOS SANTOS","payeeDocument":"***.982.247-**","amount":1900.75,"paymentDate":"06/08/2026"}
            """);

        var extraction = _analyzer.FromVision(json.RootElement);

        Assert.True(extraction.IsComplete);
        Assert.Equal("***982247**", extraction.Get("payeeCpf"));
        Assert.Equal("1900.75", extraction.Get("amount"));
    }
}
