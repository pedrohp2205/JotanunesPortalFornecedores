namespace Jotanunes.Infra.DocumentAi.Settings;

public class DocumentAnalysisSettings
{
    public const string SectionName = "DocumentAnalysis";

    public bool WorkerEnabled { get; set; }
    public int PollingIntervalSeconds { get; set; } = 10;
    public int BatchSize { get; set; } = 10;
}
