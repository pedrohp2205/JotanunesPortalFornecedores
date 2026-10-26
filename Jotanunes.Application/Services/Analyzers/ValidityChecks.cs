using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Enums;

namespace Jotanunes.Application.Services.Analyzers;

public static class ValidityChecks
{
    public const int ExpiringSoonDays = 7;

    public static void Add(
        List<AnalysisFinding> findings,
        string documentLabel,
        DateOnly? validFrom,
        DateOnly validUntil,
        DateOnly? informedExpiration,
        DateOnly today)
    {
        if (validUntil < today)
        {
            findings.Add(new AnalysisFinding(
                "EXPIRED",
                FindingSeverity.Blocking,
                $"{documentLabel} vencida em {TextPatterns.FormatDate(validUntil)}."));
        }
        else if (validFrom > today)
        {
            findings.Add(new AnalysisFinding(
                "NOT_YET_VALID",
                FindingSeverity.Warning,
                $"A validade da {documentLabel} só começa em {TextPatterns.FormatDate(validFrom.Value)}."));
        }
        else if (validUntil.DayNumber - today.DayNumber <= ExpiringSoonDays)
        {
            findings.Add(new AnalysisFinding(
                "EXPIRING_SOON",
                FindingSeverity.Warning,
                $"{documentLabel} vence em {validUntil.DayNumber - today.DayNumber} dia(s), em {TextPatterns.FormatDate(validUntil)}."));
        }

        if (informedExpiration is null)
        {
            findings.Add(new AnalysisFinding(
                "EXPIRATION_DATE_DETECTED",
                FindingSeverity.Info,
                $"Validade identificada na certidão: {TextPatterns.FormatDate(validUntil)}."));
        }
        else if (informedExpiration.Value != validUntil)
        {
            findings.Add(new AnalysisFinding(
                "EXPIRATION_DATE_MISMATCH",
                FindingSeverity.Warning,
                $"A validade informada no envio ({TextPatterns.FormatDate(informedExpiration.Value)}) difere da certidão ({TextPatterns.FormatDate(validUntil)})."));
        }
    }
}
