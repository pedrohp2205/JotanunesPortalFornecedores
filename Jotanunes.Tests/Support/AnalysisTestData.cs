using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Enums;

namespace Jotanunes.Tests.Support;

internal static class AnalysisTestData
{
    public const string CompanyCnpj = "11.222.333/0001-81";
    public const string CompanyName = "Construtora Exemplo LTDA";

    public static Document CrfDocument(DateOnly? expirationDate = null, string typeCode = "FGTS_CND")
    {
        var company = new Company(
            CompanyCnpj,
            CompanyName,
            "Construtora Exemplo",
            "contato@exemplo.com.br",
            "(79) 99999-8888",
            "Maria Souza",
            new Address("Rua Sao Cristovao", "123", "Centro", "Aracaju", "SE", "49000-000"),
            SupplierType.ManpowerLabor)
        { Id = 1 };

        var type = new DocumentType(typeCode, "Certidão Negativa de FGTS", DocumentCategory.Onboarding, SupplierType.ManpowerLabor, DocumentSubject.Company) { Id = 10 };

        var document = new Document(
            companyId: 1,
            documentTypeId: 10,
            uploadedBySupplierUserId: 5,
            category: DocumentCategory.Onboarding,
            subject: DocumentSubject.Company,
            storageKey: "companies/1/documents/abc-crf.pdf",
            originalFileName: "crf.pdf",
            contentType: "application/pdf",
            expirationDate: expirationDate)
        { Id = 100 };

        typeof(Document).GetProperty(nameof(Document.Company))!.SetValue(document, company);
        typeof(Document).GetProperty(nameof(Document.DocumentType))!.SetValue(document, type);

        return document;
    }

    public static string CrfText(
        string cnpj = CompanyCnpj,
        string corporateName = CompanyName,
        string validity = "11/07/2026 a 09/08/2026")
    {
        return $"""
            Certificado de Regularidade
            do FGTS - CRF
            Inscrição: {cnpj}
            Razão
            social: {corporateName}
            Endereço: RUA SAO CRISTOVAO 123 / CENTRO / ARACAJU / SE / 49000-000
            A Caixa Econômica Federal, no uso da atribuição que lhe confere o Art.
            7, da Lei 8.036, de 11 de maio de 1990, certifica que, nesta data, a
            empresa acima identificada encontra-se em situação regular perante o
            Fundo de Garantia do Tempo de Servico - FGTS.
            Validade: {validity}
            Certificação Número: 2026071115160835286076
            """;
    }

    public static string CrfTextLabelsFirst()
    {
        return $"""
            Certificado de Regularidade
            do FGTS - CRF
            Inscrição:
            Razão
            social:
            Endereço:
            {CompanyCnpj}
            {CompanyName}
            RUA SAO CRISTOVAO 123 / CENTRO / ARACAJU / SE / 49000-000
            Validade: 11/07/2026 a 09/08/2026
            Certificação Número: 2026071115160835286076
            """;
    }
}
