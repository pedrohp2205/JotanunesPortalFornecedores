using Jotanunes.Domain.Enums;

namespace Jotanunes.Application.DTOs.Compliance;

// Um tipo de documento na tela em que o operacional monta a solicitação.
public class DocumentOptionDto
{
    public long DocumentTypeId { get; set; }
    public string DocumentTypeCode { get; set; } = string.Empty;
    public string DocumentTypeName { get; set; } = string.Empty;
    public DocumentCategory Category { get; set; }
    public DocumentSubject Subject { get; set; }

    public bool IsConditional { get; set; }
    public string? ConditionDescription { get; set; }

    // Já exigido nesta solicitação. Em solicitação nova vem igual a Suggested.
    public bool Selected { get; set; }
    // Sugestão do sistema: o que a empresa ainda não tem entregue.
    public bool Suggested { get; set; }

    // Preenchidos só para a habilitação da empresa, que é o que ela carrega de
    // uma solicitação para outra. O resto nasce em branco a cada solicitação.
    public bool CompanyAlreadyHas { get; set; }
    public ChecklistItemStatus CompanyStatus { get; set; }
    public long? CompanyDocumentId { get; set; }
    public DateOnly? CompanyDocumentExpirationDate { get; set; }
}
