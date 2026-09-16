using Jotanunes.Domain.Exceptions;

namespace Jotanunes.Domain.Entities;

public class CompanyWorkSite : BaseEntity
{
    public long CompanyId { get; private set; }
    public Company Company { get; private set; } = null!;
    public long WorkSiteId { get; private set; }
    public WorkSite WorkSite { get; private set; } = null!;
    public int? RequiredWorkerCount { get; private set; }

    protected CompanyWorkSite() { }

    public CompanyWorkSite(long companyId, long workSiteId, int? requiredWorkerCount = null)
    {
        Validate(requiredWorkerCount);

        CompanyId = companyId;
        WorkSiteId = workSiteId;
        RequiredWorkerCount = requiredWorkerCount;
    }

    public void UpdateRequiredWorkerCount(int? requiredWorkerCount)
    {
        Validate(requiredWorkerCount);

        RequiredWorkerCount = requiredWorkerCount;
    }

    private static void Validate(int? requiredWorkerCount)
    {
        JotanunesException.When(requiredWorkerCount is < 0, "Quantidade de trabalhadores necessária não pode ser negativa.");
    }
}
