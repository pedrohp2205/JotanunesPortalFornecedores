using Jotanunes.Application.DTOs.Compliance;
using Jotanunes.Domain.Enums;

namespace Jotanunes.Application.DTOs.SupplyRequests;

public class SupplyRequestDto
{
    public long Id { get; set; }
    public long CompanyId { get; set; }
    public string CompanyCorporateName { get; set; } = string.Empty;
    public string CompanyTradeName { get; set; } = string.Empty;
    public string CompanyFormattedCnpj { get; set; } = string.Empty;
    public long WorkSiteId { get; set; }
    public string WorkSiteName { get; set; } = string.Empty;
    public int RenewalPeriodDays { get; set; }
    public SupplierType SupplierType { get; set; }
    public string SupplierTypeDescription { get; set; } = string.Empty;
    public int? RequiredWorkerCount { get; set; }
    public SupplyRequestStatus Status { get; set; }
    public string StatusDescription { get; set; } = string.Empty;
    public DateTime? ClosedAt { get; set; }
    public SupplyRequestPendingDto? Pending { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
