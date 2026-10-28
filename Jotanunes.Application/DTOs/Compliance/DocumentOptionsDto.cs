using Jotanunes.Domain.Enums;

namespace Jotanunes.Application.DTOs.Compliance;

public class DocumentOptionsDto
{
    public long? SupplyRequestId { get; set; }
    public long CompanyId { get; set; }
    public string CompanyCorporateName { get; set; } = string.Empty;
    public SupplierType SupplierType { get; set; }

    public List<DocumentOptionDto> CompanyOnboarding { get; set; } = [];
    public List<DocumentOptionDto> CompanyRecurring { get; set; } = [];
    public List<DocumentOptionDto> WorkerOnboarding { get; set; } = [];
    public List<DocumentOptionDto> WorkerRecurring { get; set; } = [];
}
