namespace Jotanunes.Application.DTOs.Notifications;

public record EmailMessage(
    IReadOnlyList<string> To,
    string Subject,
    string HtmlBody,
    string? TextBody = null);
