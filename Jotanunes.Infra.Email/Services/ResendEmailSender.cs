using System.Net.Http.Headers;
using System.Net.Http.Json;
using Jotanunes.Application.DTOs.Notifications;
using Jotanunes.Application.Interfaces;
using Jotanunes.Infra.Email.Settings;
using Microsoft.Extensions.Options;

namespace Jotanunes.Infra.Email.Services;

public class ResendEmailSender : IEmailSender
{
    private readonly HttpClient _httpClient;
    private readonly EmailSettings _settings;

    public ResendEmailSender(HttpClient httpClient, IOptions<EmailSettings> settings)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
    }

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        var from = string.IsNullOrWhiteSpace(_settings.FromName)
            ? _settings.FromAddress
            : $"{_settings.FromName} <{_settings.FromAddress}>";

        using var request = new HttpRequestMessage(HttpMethod.Post, _settings.ApiUrl)
        {
            Content = JsonContent.Create(new
            {
                from,
                to = message.To,
                subject = message.Subject,
                html = message.HtmlBody,
                text = message.TextBody
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);

        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"Resend retornou {(int)response.StatusCode}: {body}");
        }
    }
}
