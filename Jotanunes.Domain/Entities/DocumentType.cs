using Jotanunes.Domain.Enums;
using Jotanunes.Domain.Exceptions;

namespace Jotanunes.Domain.Entities;

public class DocumentType : BaseEntity
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public DocumentCategory Category { get; private set; }
    public SupplierType AppliesTo { get; private set; }
    public DocumentSubject Subject { get; private set; }
    public bool RequiresExpirationDate { get; private set; }
    public bool IsConditional { get; private set; }
    public string? ConditionDescription { get; private set; }
    public bool Active { get; private set; }

    protected DocumentType() { }

    public DocumentType(
        string code,
        string name,
        DocumentCategory category,
        SupplierType appliesTo,
        DocumentSubject subject,
        bool requiresExpirationDate = false,
        bool isConditional = false,
        string? conditionDescription = null)
    {
        Validate(code, name, appliesTo, isConditional, conditionDescription);

        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        Category = category;
        AppliesTo = appliesTo;
        Subject = subject;
        RequiresExpirationDate = requiresExpirationDate;
        IsConditional = isConditional;
        ConditionDescription = string.IsNullOrWhiteSpace(conditionDescription) ? null : conditionDescription.Trim();
        Active = true;
    }

    public void Update(
        string name,
        DocumentCategory category,
        SupplierType appliesTo,
        DocumentSubject subject,
        bool requiresExpirationDate,
        bool isConditional,
        string? conditionDescription)
    {
        Validate(Code, name, appliesTo, isConditional, conditionDescription);

        Name = name.Trim();
        Category = category;
        AppliesTo = appliesTo;
        Subject = subject;
        RequiresExpirationDate = requiresExpirationDate;
        IsConditional = isConditional;
        ConditionDescription = string.IsNullOrWhiteSpace(conditionDescription) ? null : conditionDescription.Trim();
    }

    public void Activate()
    {
        Active = true;
    }

    public void Deactivate()
    {
        Active = false;
    }

    private static void Validate(string code, string name, SupplierType appliesTo, bool isConditional, string? conditionDescription)
    {
        JotanunesException.When(string.IsNullOrWhiteSpace(code), "Código do tipo de documento não pode ser vazio.");
        JotanunesException.When(string.IsNullOrWhiteSpace(name), "Nome do tipo de documento não pode ser vazio.");

        JotanunesException.When(
            appliesTo is not (SupplierType.Material or SupplierType.ManpowerLabor or (SupplierType.Material | SupplierType.ManpowerLabor)),
            "Aplicabilidade do tipo de documento inválida.");

        JotanunesException.When(
            isConditional && string.IsNullOrWhiteSpace(conditionDescription),
            "Descrição da condição é obrigatória quando o documento é condicional.");
    }
}
