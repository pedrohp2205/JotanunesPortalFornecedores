using System.ComponentModel.DataAnnotations;

namespace Jotanunes.Worker.Models;

public class PeriodComplianceSettings
{
    public const string SectionName = "PeriodCompliance";

    [Range(1, 100)]
    public int BatchSize { get; set; } = 10;
}
