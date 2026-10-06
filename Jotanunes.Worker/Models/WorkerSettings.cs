using System.ComponentModel.DataAnnotations;

namespace Jotanunes.Worker.Models;

public class WorkerSettings
{
    public const string SectionName = "Schedules";

    [Required]
    public string DocumentAnalysisCron { get; set; } = string.Empty;
}
