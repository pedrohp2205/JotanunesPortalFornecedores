using Jotanunes.Worker.Models;
using Jotanunes.Worker.Schedules;
using Microsoft.Extensions.Options;

namespace Jotanunes.Worker.Tests.Schedules;

public class DocumentAnalysisCronScheduleTests
{
    private static DocumentAnalysisCronSchedule Schedule(string cron) =>
        new(Options.Create(new WorkerSettings { DocumentAnalysisCron = cron }));

    [Theory]
    [InlineData("-")]
    [InlineData("never")]
    [InlineData("NEVER")]
    public void Should_Be_Disabled_With_Disabled_Cron(string cron)
    {
        Assert.False(Schedule(cron).IsEnabled);
    }

    [Fact]
    public void Should_Accept_Cron_With_Seconds()
    {
        var now = new DateTimeOffset(2026, 10, 6, 9, 0, 3, TimeSpan.Zero);

        var next = Schedule("*/10 * * * * *").GetNextOccurrence(now);

        Assert.Equal(now.AddSeconds(7), next);
    }

    [Fact]
    public void Should_Accept_Standard_Cron()
    {
        var now = new DateTimeOffset(2026, 10, 6, 9, 0, 3, TimeSpan.Zero);

        var next = Schedule("*/5 * * * *").GetNextOccurrence(now);

        Assert.Equal(new DateTimeOffset(2026, 10, 6, 9, 5, 0, TimeSpan.Zero), next.ToUniversalTime());
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Should_Reject_Empty_Cron(string cron)
    {
        Assert.Throws<InvalidOperationException>(() => Schedule(cron));
    }
}
