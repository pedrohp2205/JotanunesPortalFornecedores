using Jotanunes.Domain.Enums;
using Jotanunes.Domain.Exceptions;
using Jotanunes.Domain.Validation;

namespace Jotanunes.Domain.Entities;

public class Document : BaseEntity
{
    public long CompanyId { get; private set; }
    public Company Company { get; private set; } = null!;

    public long? CompanyWorkSiteId { get; private set; }
    public CompanyWorkSite? CompanyWorkSite { get; private set; }

    public long DocumentTypeId { get; private set; }
    public DocumentType DocumentType { get; private set; } = null!;

    public long UploadedBySupplierUserId { get; private set; }
    public SupplierUser UploadedBySupplierUser { get; private set; } = null!;

    public string? WorkerName { get; private set; }
    public string? WorkerCpf { get; private set; }

    public string StorageKey { get; private set; } = string.Empty;
    public string OriginalFileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;

    public DateOnly? ReferencePeriodStart { get; private set; }
    public DateOnly? ReferencePeriodEnd { get; private set; }
    public DateOnly? ExpirationDate { get; private set; }

    public DocumentStatus Status { get; private set; }
    public string? RejectionReason { get; private set; }
    public DateTime? ReviewedAt { get; private set; }

    protected Document() { }

    public Document(
        long companyId,
        long documentTypeId,
        long uploadedBySupplierUserId,
        DocumentCategory category,
        DocumentSubject subject,
        string storageKey,
        string originalFileName,
        string contentType,
        long? companyWorkSiteId = null,
        string? workerName = null,
        string? workerCpf = null,
        DateOnly? referencePeriodStart = null,
        DateOnly? referencePeriodEnd = null,
        DateOnly? expirationDate = null)
    {
        workerCpf = string.IsNullOrWhiteSpace(workerCpf) ? null : Cpf.Normalize(workerCpf);
        Validate(category, subject, storageKey, originalFileName, contentType, companyWorkSiteId, workerName, workerCpf, referencePeriodStart, referencePeriodEnd);

        CompanyId = companyId;
        DocumentTypeId = documentTypeId;
        UploadedBySupplierUserId = uploadedBySupplierUserId;
        CompanyWorkSiteId = companyWorkSiteId;
        WorkerName = string.IsNullOrWhiteSpace(workerName) ? null : workerName.Trim();
        WorkerCpf = workerCpf;
        StorageKey = storageKey;
        OriginalFileName = originalFileName.Trim();
        ContentType = contentType.Trim();
        ReferencePeriodStart = referencePeriodStart;
        ReferencePeriodEnd = referencePeriodEnd;
        ExpirationDate = expirationDate;
        Status = DocumentStatus.Pending;
    }

    public void Approve()
    {
        JotanunesException.When(Status != DocumentStatus.Pending, "Documento já foi avaliado.");

        Status = DocumentStatus.Approved;
        RejectionReason = null;
        ReviewedAt = DateTime.UtcNow;
    }

    public void Reject(string reason)
    {
        JotanunesException.When(Status != DocumentStatus.Pending, "Documento já foi avaliado.");
        JotanunesException.When(string.IsNullOrWhiteSpace(reason), "Motivo da rejeição é obrigatório.");

        Status = DocumentStatus.Rejected;
        RejectionReason = reason.Trim();
        ReviewedAt = DateTime.UtcNow;
    }

    public bool IsExpired(DateOnly referenceDate)
    {
        return ExpirationDate.HasValue && ExpirationDate.Value < referenceDate;
    }

    private static void Validate(
        DocumentCategory category,
        DocumentSubject subject,
        string storageKey,
        string originalFileName,
        string contentType,
        long? companyWorkSiteId,
        string? workerName,
        string? workerCpf,
        DateOnly? referencePeriodStart,
        DateOnly? referencePeriodEnd)
    {
        JotanunesException.When(string.IsNullOrWhiteSpace(storageKey), "Chave de armazenamento não pode ser vazia.");
        JotanunesException.When(string.IsNullOrWhiteSpace(originalFileName), "Nome do arquivo não pode ser vazio.");
        JotanunesException.When(string.IsNullOrWhiteSpace(contentType), "Tipo do arquivo não pode ser vazio.");

        if (category == DocumentCategory.Recurring)
        {
            JotanunesException.When(companyWorkSiteId is null, "Documento recorrente precisa estar vinculado a uma solicitação (empresa e obra).");
            JotanunesException.When(
                referencePeriodStart is null || referencePeriodEnd is null,
                "Documento recorrente precisa informar o período de referência.");
            JotanunesException.When(
                referencePeriodStart is not null && referencePeriodEnd is not null && referencePeriodStart > referencePeriodEnd,
                "Período de referência inválido.");
        }
        else
        {
            JotanunesException.When(companyWorkSiteId is not null, "Documento de habilitação não deve estar vinculado a uma obra específica.");
            JotanunesException.When(
                referencePeriodStart is not null || referencePeriodEnd is not null,
                "Documento de habilitação não deve ter período de referência.");
        }

        if (subject == DocumentSubject.Worker)
        {
            JotanunesException.When(string.IsNullOrWhiteSpace(workerName), "Nome do trabalhador é obrigatório para este tipo de documento.");
            JotanunesException.When(string.IsNullOrWhiteSpace(workerCpf), "CPF do trabalhador é obrigatório para este tipo de documento.");
            JotanunesException.When(!Cpf.IsValid(workerCpf), "CPF do trabalhador inválido.");
        }
        else
        {
            JotanunesException.When(
                !string.IsNullOrWhiteSpace(workerName) || !string.IsNullOrWhiteSpace(workerCpf),
                "Documento de empresa não deve ter trabalhador vinculado.");
        }
    }
}
