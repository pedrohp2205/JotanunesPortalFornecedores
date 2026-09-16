using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Exceptions;

namespace Jotanunes.Tests;

public class CompanyWorkSiteTest
{
    [Fact]
    public void Should_Create_Link_With_Required_Worker_Count()
    {
        var link = new CompanyWorkSite(1, 1, 30);

        Assert.Equal(30, link.RequiredWorkerCount);
    }

    [Fact]
    public void Should_Create_Link_Without_Required_Worker_Count()
    {
        var link = new CompanyWorkSite(1, 1);

        Assert.Null(link.RequiredWorkerCount);
    }

    [Fact]
    public void Should_Throw_Exception_When_Required_Worker_Count_Is_Negative()
    {
        var ex = Assert.Throws<JotanunesException>(() => new CompanyWorkSite(1, 1, -1));
        Assert.Equal("Quantidade de trabalhadores necessária não pode ser negativa.", ex.Message);
    }

    [Fact]
    public void Should_Update_Required_Worker_Count()
    {
        var link = new CompanyWorkSite(1, 1, 30);

        link.UpdateRequiredWorkerCount(25);

        Assert.Equal(25, link.RequiredWorkerCount);
    }
}
