using System.Text.Json;
using Jotanunes.Application.DTOs.Analysis;
using Jotanunes.Application.Services.Analyzers;
using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Enums;
using Jotanunes.Tests.Support;

namespace Jotanunes.Tests.Application.Services.Analyzers;

public class OnboardingAnalyzersTests
{
    private static readonly DateOnly Today = new(2026, 8, 10);

    private static Document Onboarding(string code, DateOnly? expiration = null) => AnalysisTestData.CrfDocument(expiration, code);

    private static FieldExtraction Vision(Jotanunes.Application.Interfaces.IVisionAnalyzer analyzer, string json)
    {
        using var document = JsonDocument.Parse(json);
        return analyzer.FromVision(document.RootElement);
    }

    private static string CnpjCard(string status = "ATIVA", string issued = "14/07/2026", string zip = "49.000-000", string cnpj = AnalysisTestData.CompanyCnpj) =>
        $"REPÚBLICA FEDERATIVA DO BRASIL CADASTRO NACIONAL DA PESSOA JURÍDICA NÚMERO DE INSCRIÇÃO {cnpj} MATRIZ COMPROVANTE DE INSCRIÇÃO E DE SITUAÇÃO CADASTRAL "
        + "NOME EMPRESARIAL CONSTRUTORA EXEMPLO LTDA TÍTULO DO ESTABELECIMENTO (NOME DE FANTASIA) CONSTRUTORA EXEMPLO "
        + "CÓDIGO E DESCRIÇÃO DA ATIVIDADE ECONÔMICA PRINCIPAL 41.20-4-00 - Construção de edifícios CÓDIGO E DESCRIÇÃO DAS ATIVIDADES ECONÔMICAS SECUNDÁRIAS "
        + $"CEP {zip} BAIRRO/DISTRITO CENTRO SITUAÇÃO CADASTRAL {status} DATA DA SITUAÇÃO CADASTRAL 03/11/2005 Emitido no dia {issued} às 08:43:10";

    [Fact]
    public void Should_Read_Cnpj_Card()
    {
        var extraction = new CnpjCardAnalyzer().Extract(new DocumentText([CnpjCard()]));

        Assert.True(extraction.IsComplete, string.Join(", ", extraction.Missing));
        Assert.Equal("CONSTRUTORA EXEMPLO LTDA", extraction.Get("corporateName"));
        Assert.Equal("ATIVA", extraction.Get("registrationStatus"));
        Assert.Equal("49000000", extraction.Get("zipCode"));
        Assert.Equal("41.20-4-00 - Construção de edifícios", extraction.Get("mainActivity"));
        Assert.Empty(new CnpjCardAnalyzer().Validate(extraction, Onboarding("CNPJ_CARD"), Today));
    }

    [Fact]
    public void Should_Flag_Inactive_Outdated_Card_From_Another_Address()
    {
        var analyzer = new CnpjCardAnalyzer();
        var findings = analyzer.Validate(analyzer.Extract(new DocumentText([CnpjCard(status: "INAPTA", issued: "01/03/2026", zip: "56.318-750")])), Onboarding("CNPJ_CARD"), Today);

        Assert.Contains(findings, f => f.Code == "REGISTRATION_NOT_ACTIVE" && f.Severity == FindingSeverity.Blocking);
        Assert.Contains(findings, f => f.Code == "DOCUMENT_OUTDATED" && f.Severity == FindingSeverity.Warning);
        Assert.Contains(findings, f => f.Code == "ZIP_CODE_MISMATCH");
    }

    private static string FederalCnd(string kind = "NEGATIVA", string validUntil = "11/01/2027", string cnpj = AnalysisTestData.CompanyCnpj) =>
        $"MINISTÉRIO DA FAZENDA CERTIDÃO {kind} DE DÉBITOS RELATIVOS AOS TRIBUTOS FEDERAIS E À DÍVIDA ATIVA DA UNIÃO Nome: CONSTRUTORA EXEMPLO LTDA CNPJ: {cnpj} "
        + $"Emitida às 09:18:45 do dia 15/07/2026 <hora e data de Brasília>. Válida até {validUntil}. Código de controle da certidão: 7C2E.6B89.864D.BE84";

    [Theory]
    [InlineData("NEGATIVA", null)]
    [InlineData("POSITIVA COM EFEITOS DE NEGATIVA", "POSITIVE_WITH_NEGATIVE_EFFECTS")]
    [InlineData("POSITIVA", "POSITIVE_CERTIFICATE")]
    public void Should_Classify_Federal_Certificate(string kind, string? expectedCode)
    {
        var analyzer = new FederalCndAnalyzer();
        var extraction = analyzer.Extract(new DocumentText([FederalCnd(kind)]));

        var findings = analyzer.Validate(extraction, Onboarding("FEDERAL_CND", new DateOnly(2027, 1, 11)), Today);

        Assert.True(extraction.IsComplete, string.Join(", ", extraction.Missing));
        if (expectedCode is null)
        {
            Assert.Empty(findings);
        }
        else
        {
            Assert.Contains(findings, f => f.Code == expectedCode);
        }
        Assert.Equal(kind == "POSITIVA" ? FindingSeverity.Blocking : (FindingSeverity?)null,
            findings.Where(f => f.Code == "POSITIVE_CERTIFICATE").Select(f => (FindingSeverity?)f.Severity).FirstOrDefault());
    }

    [Fact]
    public void Should_Accept_Federal_Certificate_Of_A_Branch_And_Block_It_When_Expired()
    {
        var analyzer = new FederalCndAnalyzer();
        var extraction = analyzer.Extract(new DocumentText([FederalCnd(validUntil: "01/08/2026", cnpj: "11.222.333/0002-62")]));

        var findings = analyzer.Validate(extraction, Onboarding("FEDERAL_CND"), Today);

        Assert.Equal("11222333000262", extraction.Get("cnpj"));
        Assert.DoesNotContain(findings, f => f.Code == "CNPJ_MISMATCH");
        Assert.Contains(findings, f => f.Code == "EXPIRED" && f.Severity == FindingSeverity.Blocking);
        Assert.Equal(new DateOnly(2026, 8, 1), analyzer.ReadExpirationDate(extraction));
    }

    [Theory]
    [InlineData("Situação no Simples Nacional: Optante pelo Simples Nacional desde 01/01/2012", "true")]
    [InlineData("Situação no Simples Nacional: NÃO optante pelo Simples Nacional", "false")]
    public void Should_Read_Simples_Consultation(string situation, string optant)
    {
        var analyzer = new SimplesNacionalAnalyzer();
        var extraction = analyzer.Extract(new DocumentText([$"Data da consulta: 27/07/2026 09:58:36 CNPJ: {AnalysisTestData.CompanyCnpj} {situation}"]));

        Assert.True(extraction.IsComplete, string.Join(", ", extraction.Missing));
        Assert.Equal(optant, extraction.Get("isOptant"));
        Assert.Equal("27/07/2026", extraction.Get("referenceDate"));
        Assert.Equal(optant == "false", analyzer.Validate(extraction, Onboarding("SIMPLES_NACIONAL"), Today).Any(f => f.Code == "NOT_SIMPLES_OPTANT"));
    }

    [Fact]
    public void Should_Read_Pgdas_Declaration_And_Warn_When_It_Is_From_Another_Year()
    {
        var analyzer = new SimplesNacionalAnalyzer();
        var extraction = analyzer.Extract(new DocumentText([
            $"Declaratório Período de Apuração: 01/06/2025 a 30/06/2025 CNPJ Matriz: {AnalysisTestData.CompanyCnpj} Optante pelo Simples Nacional: Sim"]));

        Assert.Equal("30/06/2025", extraction.Get("referenceDate"));
        Assert.Contains(analyzer.Validate(extraction, Onboarding("SIMPLES_NACIONAL"), Today), f => f.Code == "NOT_CURRENT_YEAR");
    }

    [Fact]
    public void Should_Read_Social_Contract_Registration_And_Partners()
    {
        var analyzer = new SocialContractAnalyzer();
        var extraction = analyzer.Extract(new DocumentText([
            $"ALTERAÇÃO CONTRATUAL DA SOCIEDADE CONSTRUTORA EXEMPLO LTDA CNPJ nº {AnalysisTestData.CompanyCnpj} MARIA APARECIDA DOS SANTOS, brasileira, casada, Empresária, CPF nº {AnalysisTestData.WorkerCpf}, RG nº 123. "
            + "NIRE nº 26201906998 Cpf: 11144477735 - JOAO PEREIRA LIMA - Assinado em 09/06/2026 CERTIFICO O REGISTRO EM 13/07/2026 SOB N: 20268991480"]));

        Assert.True(extraction.IsComplete, string.Join(", ", extraction.Missing));
        Assert.Equal("CONSTRUTORA EXEMPLO LTDA", extraction.Get("corporateName"));
        Assert.Equal("13/07/2026", extraction.Get("registeredAt"));
        Assert.Equal("20268991480", extraction.Get("registrationNumber"));
        Assert.Equal(["JOAO PEREIRA LIMA", "MARIA APARECIDA DOS SANTOS"], extraction.GetList<Partner>("partners").Select(p => p.Name).Order().ToArray());
        Assert.Empty(analyzer.Validate(extraction, Onboarding("SOCIAL_CONTRACT"), Today));
    }

    [Fact]
    public void Should_Warn_When_Contract_Has_No_Registration()
    {
        var analyzer = new SocialContractAnalyzer();
        var extraction = analyzer.Extract(new DocumentText([$"CONTRATO SOCIAL DA SOCIEDADE CONSTRUTORA EXEMPLO LTDA CNPJ nº {AnalysisTestData.CompanyCnpj}"]));

        var findings = analyzer.Validate(extraction, Onboarding("SOCIAL_CONTRACT"), Today);

        Assert.Contains(findings, f => f.Code == "NOT_REGISTERED" && f.Severity == FindingSeverity.Warning);
        Assert.Contains(findings, f => f.Code == "PARTNERS_NOT_FOUND");
    }

    [Fact]
    public void Should_Flag_Address_Proof_In_Another_Name_Zip_And_Number()
    {
        var analyzer = new AddressProofAnalyzer();
        var extraction = Vision(analyzer, """
            {"isExpectedDocument":true,"detectedDocument":"conta de luz","holderName":"JOAO PEREIRA LIMA","holderDocument":"111.444.777-35",
             "street":"RUA SAO CRISTOVAO","number":"404","city":"ARACAJU","state":"SE","zipCode":"56318-750","issuedAt":"22/03/2026","isLeaseContract":false}
            """);

        var findings = analyzer.Validate(extraction, Onboarding("ADDRESS_PROOF_COMPANY"), Today);

        Assert.Equal(
            ["ADDRESS_NUMBER_MISMATCH", "DOCUMENT_OUTDATED", "HOLDER_NOT_COMPANY", "ZIP_CODE_MISMATCH"],
            findings.Select(f => f.Code).Order().ToArray());
    }

    [Fact]
    public void Should_Accept_Company_Address_Proof_And_Skip_Age_For_Lease_Contract()
    {
        var analyzer = new AddressProofAnalyzer();
        var extraction = Vision(analyzer, $$"""
            {"isExpectedDocument":true,"detectedDocument":"contrato de locação","holderName":"CONSTRUTORA EXEMPLO LTDA","holderDocument":"{{AnalysisTestData.CompanyCnpj}}",
             "street":"RUA SAO CRISTOVAO","number":"123","city":"ARACAJU","state":"SE","zipCode":"49000-000","issuedAt":"01/01/2024","isLeaseContract":true}
            """);

        Assert.Empty(analyzer.Validate(extraction, Onboarding("ADDRESS_PROOF_COMPANY"), Today));
    }

    [Fact]
    public void Should_Warn_About_Expired_Partner_Id()
    {
        var analyzer = new PartnerIdAnalyzer();
        var extraction = Vision(analyzer, $$"""
            {"isExpectedDocument":true,"detectedDocument":"CNH","documentType":"cnh","name":"MARIA APARECIDA DOS SANTOS","cpf":"{{AnalysisTestData.WorkerCpf}}","validUntil":"01/01/2026"}
            """);

        Assert.True(extraction.IsComplete, string.Join(", ", extraction.Missing));
        Assert.Equal("CNH", extraction.Get("documentType"));
        Assert.Equal("ID_EXPIRED", Assert.Single(analyzer.Validate(extraction, Onboarding("PARTNER_ID"), Today)).Code);
    }

    [Fact]
    public void Should_Check_Generic_Certificate_Holder_And_Validity()
    {
        var art = GenericCertificateAnalyzer.Defaults().Single(a => a.DocumentTypeCodes.Contains("ART"));
        var extraction = Vision(art, """
            {"isExpectedDocument":true,"detectedDocument":"ART","holderName":"OUTRA EMPRESA","holderDocument":"11.444.777/0001-61",
             "documentNumber":"PE20260001","issuer":"CREA-PE","issuedAt":"01/06/2026","validUntil":"01/07/2026"}
            """);

        var findings = art.Validate(extraction, Onboarding("ART"), Today);

        Assert.Equal("art", art.SchemaName);
        Assert.Contains(findings, f => f.Code == "CNPJ_MISMATCH");
        Assert.Contains(findings, f => f.Code == "EXPIRED");
    }
}
