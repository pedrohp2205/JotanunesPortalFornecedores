namespace Jotanunes.Domain.Enums;

// Situação documental da empresa fornecedora.
// RN: só fica Eligible com 100% da documentação obrigatória aprovada e volta a
// Blocked quando algum documento obrigatório vence.
public enum CompanyStatus
{
    PendingDocumentation = 1,
    Eligible = 2,
    NotEligible = 3,
    Blocked = 4
}
