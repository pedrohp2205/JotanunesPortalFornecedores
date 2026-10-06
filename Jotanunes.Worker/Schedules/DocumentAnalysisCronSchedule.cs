using Jotanunes.Worker.Models;
using Jotanunes.Worker.Schedules.Interface;
using Microsoft.Extensions.Options;

namespace Jotanunes.Worker.Schedules;

public sealed class DocumentAnalysisCronSchedule(IOptions<WorkerSettings> options)
    : BaseCronSchedule(options.Value.DocumentAnalysisCron), IDocumentAnalysisCronSchedule
{
}
