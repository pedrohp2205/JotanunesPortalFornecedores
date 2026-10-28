using Jotanunes.Domain.Time;

namespace Jotanunes.Tests.Domain.Time;

public class BrazilTimeTests
{
    [Fact]
    public void Should_Convert_Utc_To_Brazilian_Time()
    {
        var local = BrazilTime.FromUtc(new DateTime(2026, 10, 28, 23, 10, 0, DateTimeKind.Utc));

        Assert.Equal(new DateTime(2026, 10, 28, 20, 10, 0), local);
        Assert.Equal(DateTimeKind.Unspecified, local.Kind);
    }

    [Fact]
    public void Should_Convert_Brazilian_Time_Back_To_Utc()
    {
        var utc = BrazilTime.ToUtc(new DateTime(2026, 10, 28, 20, 10, 0));

        Assert.Equal(new DateTime(2026, 10, 28, 23, 10, 0), utc);
    }

    [Theory]
    [InlineData(2026, 10, 29, 0, 30, 2026, 10, 28)]
    [InlineData(2026, 10, 29, 2, 59, 2026, 10, 28)]
    [InlineData(2026, 10, 29, 3, 0, 2026, 10, 29)]
    [InlineData(2026, 10, 29, 12, 0, 2026, 10, 29)]
    public void Should_Keep_The_Brazilian_Day_After_The_Utc_Day_Has_Turned(
        int year, int month, int day, int hour, int minute,
        int expectedYear, int expectedMonth, int expectedDay)
    {
        var utc = new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Utc);

        Assert.Equal(new DateOnly(expectedYear, expectedMonth, expectedDay), BrazilTime.ToDateOnly(utc));
        Assert.Equal(new DateOnly(expectedYear, expectedMonth, expectedDay), BrazilTime.Today(new FixedTimeProvider(utc)));
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
