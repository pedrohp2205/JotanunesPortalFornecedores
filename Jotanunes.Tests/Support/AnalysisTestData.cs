using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Enums;

namespace Jotanunes.Tests.Support;

internal static class AnalysisTestData
{
    public const string CompanyCnpj = "11.222.333/0001-81";
    public const string CompanyName = "Construtora Exemplo LTDA";
    public const string WorkerName = "Maria Aparecida dos Santos";
    public const string WorkerCpf = "529.982.247-25";

    public static Company Company()
    {
        return new Company(
            CompanyCnpj,
            CompanyName,
            "Construtora Exemplo",
            "contato@exemplo.com.br",
            "(79) 99999-8888",
            "Maria Souza",
            new Address("Rua Sao Cristovao", "123", "Centro", "Aracaju", "SE", "49000-000"),
            SupplierType.ManpowerLabor)
        { Id = 1 };
    }

    public const string WorkSiteName = "Residencial Aurora";

    public static Document RecurringDocument(string typeCode, string typeName, DocumentSubject subject, bool withWorker = true)
    {
        var type = new DocumentType(typeCode, typeName, DocumentCategory.Recurring, SupplierType.ManpowerLabor, subject) { Id = 30 };
        var supplyRequest = new SupplyRequest(1, 9, SupplierType.ManpowerLabor) { Id = 3 };
        typeof(SupplyRequest).GetProperty(nameof(SupplyRequest.WorkSite))!.SetValue(supplyRequest, new WorkSite(WorkSiteName) { Id = 9 });

        var document = new Document(
            companyId: 1,
            documentTypeId: 30,
            uploadedBySupplierUserId: 5,
            category: DocumentCategory.Recurring,
            subject: subject,
            storageKey: "companies/1/documents/abc.pdf",
            originalFileName: "documento.pdf",
            contentType: "application/pdf",
            supplyRequestId: 3,
            workerId: subject == DocumentSubject.Worker ? 7 : null,
            referencePeriodStart: new DateOnly(2026, 7, 1),
            referencePeriodEnd: new DateOnly(2026, 7, 31))
        { Id = 300 };

        typeof(Document).GetProperty(nameof(Document.Company))!.SetValue(document, Company());
        typeof(Document).GetProperty(nameof(Document.DocumentType))!.SetValue(document, type);
        typeof(Document).GetProperty(nameof(Document.SupplyRequest))!.SetValue(document, supplyRequest);
        if (subject == DocumentSubject.Worker && withWorker)
        {
            typeof(Document).GetProperty(nameof(Document.Worker))!.SetValue(document, new Worker(1, WorkerName, WorkerCpf) { Id = 7 });
        }

        return document;
    }

    public static Document ReceiptDocument(bool withWorker = true)
    {
        var type = new DocumentType("PAYMENT_RECEIPT", "Recibo de Pagamento", DocumentCategory.Recurring, SupplierType.ManpowerLabor, DocumentSubject.Worker) { Id = 20 };
        var worker = new Worker(1, WorkerName, WorkerCpf) { Id = 7 };

        var document = new Document(
            companyId: 1,
            documentTypeId: 20,
            uploadedBySupplierUserId: 5,
            category: DocumentCategory.Recurring,
            subject: DocumentSubject.Worker,
            storageKey: "companies/1/documents/abc-recibo.pdf",
            originalFileName: "recibo.pdf",
            contentType: "application/pdf",
            supplyRequestId: 3,
            workerId: 7,
            referencePeriodStart: new DateOnly(2026, 7, 1),
            referencePeriodEnd: new DateOnly(2026, 7, 31))
        { Id = 200 };

        typeof(Document).GetProperty(nameof(Document.Company))!.SetValue(document, Company());
        typeof(Document).GetProperty(nameof(Document.DocumentType))!.SetValue(document, type);
        if (withWorker)
        {
            typeof(Document).GetProperty(nameof(Document.Worker))!.SetValue(document, worker);
        }

        return document;
    }

    public static string ReceiptJson(
        string? employeeCpf = WorkerCpf,
        string employeeName = "MARIA APARECIDA DOS SANTOS",
        string? employerCnpj = CompanyCnpj,
        string? competence = "07/2026",
        string netPay = "1900.75",
        string employeeSigned = "false",
        bool isExpectedDocument = true,
        string? detectedDocument = "recibo de pagamento")
    {
        return $$"""
            {
              "isExpectedDocument": {{(isExpectedDocument ? "true" : "false")}},
              "detectedDocument": {{Json(detectedDocument)}},
              "employerName": "CONSTRUTORA EXEMPLO LTDA",
              "employerCnpj": {{Json(employerCnpj)}},
              "employeeName": {{Json(employeeName)}},
              "employeeCpf": {{Json(employeeCpf)}},
              "competence": {{Json(competence)}},
              "netPay": {{netPay}},
              "employeeSigned": {{employeeSigned}}
            }
            """;
    }

    public static string CrfJson(
        bool isExpectedDocument = true,
        string? detectedDocument = "CRF do FGTS",
        string? validUntil = "09/08/2026")
    {
        return $$"""
            {
              "isExpectedDocument": {{(isExpectedDocument ? "true" : "false")}},
              "detectedDocument": {{Json(detectedDocument)}},
              "cnpj": "{{CompanyCnpj}}",
              "corporateName": "{{CompanyName}}",
              "validFrom": "11/07/2026",
              "validUntil": {{Json(validUntil)}},
              "certificationNumber": "2026071115160835286076"
            }
            """;
    }

    private static string Json(string? value) => value is null ? "null" : $"\"{value}\"";


    public static Document CrfDocument(DateOnly? expirationDate = null, string typeCode = "FGTS_CND")
    {
        var company = Company();

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
