using System.Text;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using Jotanunes.Application.DTOs.Notifications;
using Jotanunes.Application.Interfaces;
using Jotanunes.Application.Settings;
using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Enums;
using Jotanunes.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jotanunes.Application.Services;

public class SupplierNotificationService : ISupplierNotificationService
{
    // Preserva acentos (o padrão escaparia tudo fora do Basic Latin) e escapa <, >, & e aspas.
    private static readonly HtmlEncoder Encoder = HtmlEncoder.Create(UnicodeRanges.All);

    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailSender _emailSender;
    private readonly NotificationSettings _settings;
    private readonly ILogger<SupplierNotificationService> _logger;

    public SupplierNotificationService(
        IUnitOfWork unitOfWork,
        IEmailSender emailSender,
        IOptions<NotificationSettings> settings,
        ILogger<SupplierNotificationService> logger)
    {
        _unitOfWork = unitOfWork;
        _emailSender = emailSender;
        _settings = settings.Value;
        _logger = logger;
    }

    public Task DocumentApproved(Document document)
    {
        return NotifyCompany(
            document.Company,
            $"Documento aprovado: {document.DocumentType.Name}",
            "Documento aprovado",
            new[]
            {
                $"O documento \"{document.DocumentType.Name}\"{DescribeContext(document)} foi aprovado."
            });
    }

    public Task DocumentRejected(Document document)
    {
        return NotifyCompany(
            document.Company,
            $"Documento rejeitado: {document.DocumentType.Name}",
            "Documento rejeitado",
            new[]
            {
                $"O documento \"{document.DocumentType.Name}\"{DescribeContext(document)} foi rejeitado.",
                $"Motivo: {document.RejectionReason}",
                "Acesse o portal e envie o documento novamente."
            });
    }

    public Task CompanyEligible(Company company)
    {
        return NotifyCompany(
            company,
            "Habilitação concluída",
            "Habilitação concluída",
            new[]
            {
                $"Todos os documentos de habilitação de {company.CorporateName} foram aprovados. A empresa está habilitada como fornecedora."
            });
    }

    public Task SupplyRequestCreated(SupplyRequest supplyRequest)
    {
        return NotifyCompany(
            supplyRequest.Company,
            $"Nova solicitação: {supplyRequest.WorkSite.Name}",
            "Nova solicitação de fornecimento",
            new[]
            {
                $"Foi aberta uma solicitação de {Label(supplyRequest.SupplierType)} para a obra {supplyRequest.WorkSite.Name}.",
                "Acesse o portal para enviar a documentação exigida."
            });
    }

    public Task SupplyRequestCompleted(SupplyRequest supplyRequest)
    {
        return NotifyCompany(
            supplyRequest.Company,
            $"Solicitação concluída: {supplyRequest.WorkSite.Name}",
            "Solicitação concluída",
            new[]
            {
                $"A solicitação de {Label(supplyRequest.SupplierType)} para a obra {supplyRequest.WorkSite.Name} foi concluída."
            });
    }

    public Task SupplyRequestCancelled(SupplyRequest supplyRequest)
    {
        return NotifyCompany(
            supplyRequest.Company,
            $"Solicitação cancelada: {supplyRequest.WorkSite.Name}",
            "Solicitação cancelada",
            new[]
            {
                $"A solicitação de {Label(supplyRequest.SupplierType)} para a obra {supplyRequest.WorkSite.Name} foi cancelada."
            });
    }

    public Task Welcome(SupplierUser user, string temporaryPassword)
    {
        return Send(
            user.Email,
            "Seu acesso ao Portal do Fornecedor",
            "Bem-vindo ao Portal do Fornecedor",
            new[]
            {
                $"Olá, {user.Name}. Seu acesso ao portal foi criado.",
                $"Login: {user.Email}",
                $"Senha temporária: {temporaryPassword}",
                "Você deverá trocar a senha no primeiro acesso."
            });
    }

    public Task TemporaryPasswordIssued(SupplierUser user, string temporaryPassword)
    {
        return Send(
            user.Email,
            "Sua senha foi redefinida",
            "Senha redefinida",
            new[]
            {
                $"Olá, {user.Name}. A Jotanunes redefiniu a sua senha de acesso ao portal.",
                $"Login: {user.Email}",
                $"Senha temporária: {temporaryPassword}",
                "Você deverá trocar a senha no próximo acesso."
            });
    }

    public Task PasswordResetRequested(SupplierUser user, string token)
    {
        var link = BuildResetLink(user.Email, token);

        var lines = new List<string>
        {
            $"Olá, {user.Name}. Recebemos um pedido para redefinir a sua senha do Portal do Fornecedor.",
            $"O link vale por {SupplierUser.PasswordResetTokenMinutes} minutos e só pode ser usado uma vez.",
            "Se você não fez esse pedido, ignore este e-mail: a sua senha continua a mesma."
        };

        if (link is null)
        {
            lines.Insert(1, $"Código de redefinição: {token}");
        }

        return Send(
            user.Email,
            "Redefinição de senha",
            "Redefinição de senha",
            lines,
            link is null ? null : ("Redefinir minha senha", link));
    }

    private async Task NotifyCompany(Company company, string subject, string title, IReadOnlyList<string> lines)
    {
        try
        {
            var recipients = await GetRecipients(company);
            if (recipients.Count == 0)
            {
                return;
            }

            await _emailSender.SendAsync(Build(recipients, subject, title, lines));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao enviar notificação '{Subject}' para a empresa {CompanyId}", subject, company.Id);
        }
    }

    private async Task Send(
        string recipient,
        string subject,
        string title,
        IReadOnlyList<string> lines,
        (string Text, string Url)? action = null)
    {
        try
        {
            await _emailSender.SendAsync(Build(new[] { recipient }, subject, title, lines, action));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao enviar notificação '{Subject}' para {Recipient}", subject, recipient);
        }
    }

    // Contato principal da empresa + usuários ativos do portal, sem duplicados.
    private async Task<List<string>> GetRecipients(Company company)
    {
        var users = await _unitOfWork.SupplierUserRepository.GetByCompany(company.Id);

        return users
            .Where(u => u.Active)
            .Select(u => u.Email)
            .Prepend(company.Email)
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private string? BuildResetLink(string email, string token)
    {
        if (string.IsNullOrWhiteSpace(_settings.PortalUrl))
        {
            return null;
        }

        var path = _settings.ResetPasswordPath.StartsWith('/') ? _settings.ResetPasswordPath : "/" + _settings.ResetPasswordPath;
        return $"{_settings.PortalUrl.TrimEnd('/')}{path}?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";
    }

    private EmailMessage Build(
        IReadOnlyList<string> recipients,
        string subject,
        string title,
        IReadOnlyList<string> lines,
        (string Text, string Url)? action = null)
    {
        var html = new StringBuilder();
        html.Append("<div style=\"font-family:Arial,sans-serif;max-width:600px;margin:0 auto;color:#222\">");
        html.Append($"<h2>{Encoder.Encode(title)}</h2>");
        foreach (var line in lines)
        {
            html.Append($"<p>{Encoder.Encode(line)}</p>");
        }

        var text = new StringBuilder();
        text.AppendLine(title);
        text.AppendLine();
        foreach (var line in lines)
        {
            text.AppendLine(line);
        }

        var linkText = action?.Text ?? "Acessar o portal";
        var linkUrl = action?.Url ?? _settings.PortalUrl;

        if (!string.IsNullOrWhiteSpace(linkUrl))
        {
            html.Append($"<p><a href=\"{Encoder.Encode(linkUrl)}\">{Encoder.Encode(linkText)}</a></p>");
            text.AppendLine();
            text.AppendLine($"{linkText}: {linkUrl}");
        }

        html.Append("</div>");

        return new EmailMessage(recipients, subject, html.ToString(), text.ToString());
    }

    private static string DescribeContext(Document document)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(document.WorkerName))
        {
            parts.Add($"do trabalhador {document.WorkerName}");
        }

        if (document.SupplyRequest is not null)
        {
            parts.Add($"da obra {document.SupplyRequest.WorkSite.Name}");
        }

        return parts.Count == 0 ? string.Empty : " " + string.Join(" ", parts);
    }

    private static string Label(SupplierType type)
    {
        return type == SupplierType.ManpowerLabor ? "mão de obra" : "material";
    }
}
