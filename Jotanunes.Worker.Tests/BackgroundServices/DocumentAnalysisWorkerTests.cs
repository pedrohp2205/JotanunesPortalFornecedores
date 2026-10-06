using Jotanunes.Application.Interfaces;
using Jotanunes.Worker.BackgroundServices;
using Jotanunes.Worker.Models;
using Jotanunes.Worker.Schedules.Interface;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Jotanunes.Worker.Tests.BackgroundServices;

public class DocumentAnalysisWorkerTests
{
    private readonly Mock<IDocumentAnalysisService> _service = new();
    private readonly Mock<IDocumentAnalysisCronSchedule> _schedule = new();

    private DocumentAnalysisWorker Worker(int batchSize = 2)
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => _service.Object);

        return new DocumentAnalysisWorker(
            _schedule.Object,
            services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new DocumentAnalysisSettings { BatchSize = batchSize }),
            NullLogger<DocumentAnalysisWorker>.Instance);
    }

    private void RunOnce()
    {
        var calls = 0;
        _schedule.SetupGet(s => s.IsEnabled).Returns(true);
        _schedule.Setup(s => s.GetNextOccurrence(It.IsAny<DateTimeOffset>()))
            .Returns((DateTimeOffset now) => Interlocked.Increment(ref calls) == 1 ? now.AddMilliseconds(20) : now.AddHours(1));
    }

    private static async Task RunUntil(DocumentAnalysisWorker worker, Task done)
    {
        var ct = TestContext.Current.CancellationToken;
        await worker.StartAsync(ct);
        var finished = await Task.WhenAny(done, Task.Delay(3000, ct)) == done;
        await worker.StopAsync(ct);
        Assert.True(finished, "A execução agendada não terminou dentro do prazo.");
    }

    [Fact]
    public async Task Should_Drain_Every_Pending_Batch_In_One_Run()
    {
        RunOnce();
        var drained = new TaskCompletionSource();
        _service.SetupSequence(s => s.GetPendingIds(2))
            .ReturnsAsync([1, 2])
            .ReturnsAsync([3])
            .ReturnsAsync(() => { drained.TrySetResult(); return []; });

        await RunUntil(Worker(), drained.Task);

        _service.Verify(s => s.Analyze(1, It.IsAny<CancellationToken>()), Times.Once);
        _service.Verify(s => s.Analyze(2, It.IsAny<CancellationToken>()), Times.Once);
        _service.Verify(s => s.Analyze(3, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_Keep_Analyzing_Batch_When_One_Analysis_Fails_And_Wait_For_Next_Run()
    {
        RunOnce();
        var lastAnalyzed = new TaskCompletionSource();
        _service.Setup(s => s.GetPendingIds(2)).ReturnsAsync([1, 2]);
        _service.Setup(s => s.Analyze(1, It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("banco fora do ar"));
        _service.Setup(s => s.Analyze(2, It.IsAny<CancellationToken>())).Callback(() => lastAnalyzed.TrySetResult()).Returns(Task.CompletedTask);

        await RunUntil(Worker(), lastAnalyzed.Task);

        _service.Verify(s => s.GetPendingIds(2), Times.Once);
    }

    [Fact]
    public async Task Should_Not_Run_When_Schedule_Is_Disabled()
    {
        _schedule.SetupGet(s => s.IsEnabled).Returns(false);
        var worker = Worker();

        await worker.StartAsync(TestContext.Current.CancellationToken);
        await (worker.ExecuteTask ?? Task.CompletedTask);
        await worker.StopAsync(TestContext.Current.CancellationToken);

        _service.Verify(s => s.GetPendingIds(It.IsAny<int>()), Times.Never);
        _schedule.Verify(s => s.GetNextOccurrence(It.IsAny<DateTimeOffset>()), Times.Never);
    }
}
