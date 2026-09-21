using Jotanunes.Application.DTOs.Notifications;

namespace Jotanunes.Application.Interfaces;

public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
