using Jotanunes.Domain.Enums;

namespace Jotanunes.Domain.Filters;

public class WorkerFilter
{
    public long? CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Cpf { get; set; } = string.Empty;
    public bool? Active { get; set; }
    public long? SupplyRequestId { get; set; }
    public WorkerOrderBy? OrderBy { get; set; }
    public OrderDirection? OrderDirection { get; set; }
}
