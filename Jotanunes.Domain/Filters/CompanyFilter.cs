using Jotanunes.Domain.Enums;

namespace Jotanunes.Domain.Filters;

public class CompanyFilter
{
    public string CorporateName { get; set; } = string.Empty;
    public string TradeName { get; set; } = string.Empty;
    public string Cnpj { get; set; } = string.Empty;
    public CompanyStatus? Status { get; set; }
}
