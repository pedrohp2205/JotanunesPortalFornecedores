using Jotanunes.Domain.Exceptions;

namespace Jotanunes.Domain.Entities;

public class WorkSite : BaseEntity
{
    public const int DefaultRenewalPeriodDays = 30;

    public string Name { get; private set; } = string.Empty;
    public int RenewalPeriodDays { get; private set; }
    public List<CompanyWorkSite> Companies { get; private set; } = [];

    protected WorkSite() { }

    public WorkSite(string name, int renewalPeriodDays = DefaultRenewalPeriodDays)
    {
        Validate(name, renewalPeriodDays);

        Name = name.Trim();
        RenewalPeriodDays = renewalPeriodDays;
    }

    public void Update(string name, int renewalPeriodDays)
    {
        Validate(name, renewalPeriodDays);

        Name = name.Trim();
        RenewalPeriodDays = renewalPeriodDays;
    }

    private static void Validate(string name, int renewalPeriodDays)
    {
        JotanunesException.When(string.IsNullOrWhiteSpace(name), "Nome da obra não pode ser vazio.");
        JotanunesException.When(renewalPeriodDays <= 0, "Período de renovação deve ser maior que zero.");
    }
}
