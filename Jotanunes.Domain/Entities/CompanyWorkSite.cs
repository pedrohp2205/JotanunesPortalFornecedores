namespace Jotanunes.Domain.Entities;

public class CompanyWorkSite : BaseEntity
{
    public long CompanyId { get; private set; }
    public Company Company { get; private set; } = null!;
    public long WorkSiteId { get; private set; }
    public WorkSite WorkSite { get; private set; } = null!;

    protected CompanyWorkSite() { }

    public CompanyWorkSite(long companyId, long workSiteId)
    {
        CompanyId = companyId;
        WorkSiteId = workSiteId;
    }
}
