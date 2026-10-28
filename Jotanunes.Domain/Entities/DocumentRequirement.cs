using Jotanunes.Domain.Exceptions;

namespace Jotanunes.Domain.Entities;

// Tipo de documento que a Jotanunes exigiu de uma solicitação específica.
// Antes o checklist cobrava todo tipo aplicável ao fornecimento; agora quem
// decide é o operacional, e é esta tabela que diz o que o fornecedor deve enviar.
public class DocumentRequirement : BaseEntity
{
    public long SupplyRequestId { get; private set; }
    public SupplyRequest SupplyRequest { get; private set; } = null!;
    public long DocumentTypeId { get; private set; }
    public DocumentType DocumentType { get; private set; } = null!;

    protected DocumentRequirement() { }

    public DocumentRequirement(long supplyRequestId, long documentTypeId)
    {
        JotanunesException.When(documentTypeId <= 0, "Tipo de documento inválido.");

        SupplyRequestId = supplyRequestId;
        DocumentTypeId = documentTypeId;
    }
}
