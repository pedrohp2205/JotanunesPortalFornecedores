using Jotanunes.Application.Interfaces;
using Jotanunes.Worker.BackgroundServices;
using Jotanunes.Worker.Models;
using Jotanunes.Worker.Schedules.Interface;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Jotanunes.Worker.Tests.BackgroundServices;

public class PeriodComplianceWorkerTests
{
    [Fact]
    public async Task Should_Recalculate_Every_Pending_Report_When_Schedule_Fires()
    {
        var service = new Mock<IPeriodComplianceService>();
        var done = new TaskCompletionSource();
        service.SetupSequence(s => s.GetPendingIds(10))
            .ReturnsAsync([1, 2])
            .ReturnsAsync(() => { done.TrySetResult(); return []; });

        var calls = 0;
        var schedule = new Mock<IPeriodComplianceCronSchedule>();
        schedule.SetupGet(s => s.IsEnabled).Returns(true);
        schedule.Setup(s => s.GetNextOccurrence(It.IsAny<DateTimeOffset>()))
            .Returns((DateTimeOffset now) => Interlocked.Increment(ref calls) == 1 ? now.AddMilliseconds(20) : now.AddHours(1));

        var services = new ServiceCollection();
        services.AddScoped(_ => service.Object);

        var worker = new PeriodComplianceWorker(
            schedule.Object,
            services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new PeriodComplianceSettings()),
            NullLogger<PeriodComplianceWorker>.Instance);

        var ct = TestContext.Current.CancellationToken;
        await worker.StartAsync(ct);
        var finished = await Task.WhenAny(done.Task, Task.Delay(3000, ct)) == done.Task;
        await worker.StopAsync(ct);

        Assert.True(finished);
        service.Verify(s => s.Recalculate(1, It.IsAny<CancellationToken>()), Times.Once);
        service.Verify(s => s.Recalculate(2, It.IsAny<CancellationToken>()), Times.Once);
    }
}
