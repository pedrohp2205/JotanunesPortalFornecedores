using Jotanunes.Application.DTOs.Notifications;
using Jotanunes.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Jotanunes.Infra.Email.Services;

// Usado quando o envio está desabilitado (Email:Enabled=false): apenas registra a mensagem no log.
public class LogEmailSender : IEmailSender
{
    private readonly ILogger<LogEmailSender> _logger;

    public LogEmailSender(ILogger<LogEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "E-mail (envio desabilitado) para {Recipients} | Assunto: {Subject}\n{Body}",
            string.Join(", ", message.To),
            message.Subject,
            message.TextBody ?? message.HtmlBody);

        return Task.CompletedTask;
    }
}
