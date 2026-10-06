using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Enums;
using Jotanunes.Domain.Validation;

namespace Jotanunes.Application.Services.Analyzers;

public static class AnalysisChecks
{
    public static void WorkerCpf(List<AnalysisFinding> findings, string? cpf, Worker? worker, string documentLabel)
    {
        if (worker is null || cpf is null)
        {
            return;
        }

        if (cpf.Contains('*'))
        {
            findings.Add(TextPatterns.MaskedCpfMatches(cpf, worker.Cpf)
                ? new AnalysisFinding(
                    "CPF_PARTIALLY_HIDDEN",
                    FindingSeverity.Info,
                    $"O CPF aparece parcialmente oculto no {documentLabel}; os dígitos visíveis conferem com {worker.Name}.")
                : new AnalysisFinding(
                    "WORKER_CPF_MISMATCH",
                    FindingSeverity.Blocking,
                    $"Os dígitos visíveis do CPF no {documentLabel} não conferem com {worker.Name} ({Cpf.Format(worker.Cpf)})."));
            return;
        }

        if (cpf != worker.Cpf)
        {
            findings.Add(new AnalysisFinding(
                "WORKER_CPF_MISMATCH",
                FindingSeverity.Blocking,
                $"O {documentLabel} é do CPF {Cpf.Format(cpf)}, mas o documento foi enviado para {worker.Name} ({Cpf.Format(worker.Cpf)})."));
        }
    }

    public static void WorkerName(List<AnalysisFinding> findings, string? name, Worker? worker, string documentLabel)
    {
        if (worker is null || name is null || TextPatterns.NormalizeName(name) == TextPatterns.NormalizeName(worker.Name))
        {
            return;
        }

        findings.Add(new AnalysisFinding(
            "WORKER_NAME_MISMATCH",
            FindingSeverity.Warning,
            $"Nome no {documentLabel} (\"{name}\") difere do trabalhador cadastrado (\"{worker.Name}\")."));
    }

    public static void CompanyCnpj(List<AnalysisFinding> findings, string? cnpj, Company company, string documentLabel, string code = "CNPJ_MISMATCH")
    {
        if (cnpj is null || cnpj == company.Cnpj)
        {
            return;
        }

        findings.Add(new AnalysisFinding(
            code,
            FindingSeverity.Blocking,
            $"O {documentLabel} é do CNPJ {Cnpj.Format(cnpj)}, mas a empresa cadastrada é {Cnpj.Format(company.Cnpj)}."));
    }

    public static void CompanyCnpjRoot(List<AnalysisFinding> findings, string? cnpjRoot, Company company, string documentLabel)
    {
        if (cnpjRoot is null || company.Cnpj.StartsWith(cnpjRoot, StringComparison.Ordinal))
        {
            return;
        }

        findings.Add(new AnalysisFinding(
            "CNPJ_MISMATCH",
            FindingSeverity.Blocking,
            $"O {documentLabel} é da raiz de CNPJ {cnpjRoot}, mas a empresa cadastrada é {Cnpj.Format(company.Cnpj)}."));
    }

    public static void CompetenceInPeriod(List<AnalysisFinding> findings, DateOnly? competence, Document document, string documentLabel)
    {
        if (competence is not { } month || document.ReferencePeriodStart is not { } periodStart || document.ReferencePeriodEnd is not { } periodEnd)
        {
            return;
        }

        var monthEnd = month.AddMonths(1).AddDays(-1);
        if (month <= periodEnd && monthEnd >= periodStart)
        {
            return;
        }

        findings.Add(new AnalysisFinding(
            "COMPETENCE_OUT_OF_PERIOD",
            FindingSeverity.Blocking,
            $"A competência do {documentLabel} ({TextPatterns.FormatCompetence(month)}) está fora do período do envio ({TextPatterns.FormatDate(periodStart)} a {TextPatterns.FormatDate(periodEnd)})."));
    }

    public static void PeriodOverlaps(List<AnalysisFinding> findings, DateOnly? start, DateOnly? end, Document document, string documentLabel)
    {
        if (start is not { } from || end is not { } until || document.ReferencePeriodStart is not { } periodStart || document.ReferencePeriodEnd is not { } periodEnd)
        {
            return;
        }

        if (from <= periodEnd && until >= periodStart)
        {
            return;
        }

        findings.Add(new AnalysisFinding(
            "PERIOD_OUT_OF_RANGE",
            FindingSeverity.Blocking,
            $"O período do {documentLabel} ({TextPatterns.FormatDate(from)} a {TextPatterns.FormatDate(until)}) está fora do período do envio ({TextPatterns.FormatDate(periodStart)} a {TextPatterns.FormatDate(periodEnd)})."));
    }

    public static void CorporateName(List<AnalysisFinding> findings, string? name, Company company, string documentLabel)
    {
        if (name is null)
        {
            return;
        }

        var normalized = WithoutSuffixes(TextPatterns.NormalizeName(name));
        if (normalized == WithoutSuffixes(TextPatterns.NormalizeName(company.CorporateName)))
        {
            return;
        }

        findings.Add(new AnalysisFinding(
            "CORPORATE_NAME_MISMATCH",
            FindingSeverity.Warning,
            $"Nome no {documentLabel} (\"{name}\") difere da razão social cadastrada (\"{company.CorporateName}\")."));
    }

    public static void IssuedRecently(List<AnalysisFinding> findings, DateOnly? issuedAt, DateOnly today, int maxAgeDays, string documentLabel)
    {
        if (issuedAt is not { } issued || today.DayNumber - issued.DayNumber <= maxAgeDays)
        {
            return;
        }

        findings.Add(new AnalysisFinding(
            "DOCUMENT_OUTDATED",
            FindingSeverity.Warning,
            $"O {documentLabel} foi emitido em {TextPatterns.FormatDate(issued)}, há mais de {maxAgeDays} dias."));
    }

    private static string WithoutSuffixes(string name)
    {
        var words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
        while (words.Count > 1 && words[^1] is "LTDA" or "ME" or "EPP" or "EIRELI" or "SA" or "S" or "A")
        {
            words.RemoveAt(words.Count - 1);
        }

        return string.Join(' ', words);
    }
}
