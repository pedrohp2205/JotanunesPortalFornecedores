using Jotanunes.Domain.Entities;

namespace Jotanunes.Tests;

public class WorkSiteTest
{
    [Fact]
    public void Should_Return_Full_Month_As_Current_Period_For_Monthly_Renewal()
    {
        var workSite = new WorkSite("Alta Vista", 30);

        var period = workSite.GetCurrentPeriod(new DateOnly(2026, 7, 15));

        Assert.Equal(new DateOnly(2026, 7, 1), period.Start);
        Assert.Equal(new DateOnly(2026, 7, 31), period.End);
    }

    [Fact]
    public void Should_Return_First_Half_As_Current_Period_For_Biweekly_Renewal()
    {
        var workSite = new WorkSite("Petrolina", 15);

        var period = workSite.GetCurrentPeriod(new DateOnly(2026, 7, 10));

        Assert.Equal(new DateOnly(2026, 7, 1), period.Start);
        Assert.Equal(new DateOnly(2026, 7, 15), period.End);
    }

    [Fact]
    public void Should_Return_Second_Half_As_Current_Period_For_Biweekly_Renewal()
    {
        var workSite = new WorkSite("Petrolina", 15);

        var period = workSite.GetCurrentPeriod(new DateOnly(2026, 7, 20));

        Assert.Equal(new DateOnly(2026, 7, 16), period.Start);
        Assert.Equal(new DateOnly(2026, 7, 31), period.End);
    }

    [Fact]
    public void Should_Return_Second_Half_Ending_On_Last_Day_Of_Short_Month()
    {
        var workSite = new WorkSite("Petrolina", 15);

        var period = workSite.GetCurrentPeriod(new DateOnly(2026, 2, 20));

        Assert.Equal(new DateOnly(2026, 2, 16), period.Start);
        Assert.Equal(new DateOnly(2026, 2, 28), period.End);
    }
}
