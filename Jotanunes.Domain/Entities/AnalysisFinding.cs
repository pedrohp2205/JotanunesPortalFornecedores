using Jotanunes.Domain.Enums;

namespace Jotanunes.Domain.Entities;

public class AnalysisFinding
{
    public string Code { get; private set; } = string.Empty;
    public FindingSeverity Severity { get; private set; }
    public string Message { get; private set; } = string.Empty;

    protected AnalysisFinding() { }

    public AnalysisFinding(string code, FindingSeverity severity, string message)
    {
        Code = code;
        Severity = severity;
        Message = message;
    }
}
