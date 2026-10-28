using Jotanunes.Application.DTOs.Compliance;
using Jotanunes.Domain.Entities;

namespace Jotanunes.Application.Interfaces;

public interface IDocumentComplianceService
{
    Task<ComplianceChecklistDto> GetChecklist(long supplyRequestId, long? companyId = null);
    // Pendências da solicitação no período corrente; null quando está em dia.
    Task<SupplyRequestPendingDto?> GetPending(long supplyRequestId);
    Task<Dictionary<long, SupplyRequestPendingDto>> GetPendingBatch(IReadOnlyCollection<SupplyRequest> supplyRequests);
    Task<bool> IsOnboardingComplete(long companyId);
    // Tipos aplicáveis para o operacional escolher, com o que a empresa já entregou.
    Task<DocumentOptionsDto> GetDocumentOptions(long companyId, Domain.Enums.SupplierType supplierType, long? supplyRequestId = null);
}
