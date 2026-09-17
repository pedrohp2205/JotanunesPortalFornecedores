using Jotanunes.Application.DTOs.Compliance;

namespace Jotanunes.Application.Interfaces;

public interface IDocumentComplianceService
{
    Task<ComplianceChecklistDto> GetChecklist(long companyWorkSiteId, long? companyId = null);
    Task<List<OverdueCompanyWorkSiteDto>> GetOverdue();
    Task<bool> IsOnboardingComplete(long companyId);
}
