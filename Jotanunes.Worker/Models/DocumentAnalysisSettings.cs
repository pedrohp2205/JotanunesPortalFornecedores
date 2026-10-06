using System.ComponentModel.DataAnnotations;

namespace Jotanunes.Worker.Models;

public class DocumentAnalysisSettings
{
    public const string SectionName = "DocumentAnalysis";

    [Range(1, 100)]
    public int BatchSize { get; set; } = 10;
}
