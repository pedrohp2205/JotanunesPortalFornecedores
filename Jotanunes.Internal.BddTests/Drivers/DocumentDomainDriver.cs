using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Enums;

namespace Jotanunes.Internal.BddTests.Drivers;

public class DocumentDomainDriver
{
    public static Document CriarDocumentoHabilitacao(long companyId, long documentTypeId, long uploadedBySupplierUserId)
    {
        return new Document(
            companyId,
            documentTypeId,
            uploadedBySupplierUserId,
            DocumentCategory.Onboarding,
            DocumentSubject.Company,
            $"companies/{companyId}/documents/{Guid.NewGuid()}-documento.pdf",
            "documento.pdf",
            "application/pdf");
    }
}
