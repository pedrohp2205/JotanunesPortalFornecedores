using Jotanunes.Application.DTOs.Compliance;

namespace Jotanunes.Application.Interfaces;

public interface IDocumentComplianceService
{
    Task<ComplianceChecklistDto> GetChecklist(long supplyRequestId, long? companyId = null);
    Task<List<OverdueSupplyRequestDto>> GetOverdue();
    // Pendências da solicitação no período corrente; null quando está em dia.
    Task<OverdueSupplyRequestDto?> GetPending(long supplyRequestId);
    Task<bool> IsOnboardingComplete(long companyId);
}
