using System.Net;
using System.Text.Json;
using Jotanunes.Application.DTOs.Notifications;
using Jotanunes.Infra.Email.Services;
using Jotanunes.Infra.Email.Settings;
using Microsoft.Extensions.Options;

namespace Jotanunes.Tests;

public class ResendEmailSenderTest
{
    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        private readonly string _responseBody;

        public HttpRequestMessage? Request { get; private set; }
        public string? RequestBody { get; private set; }

        public StubHandler(HttpStatusCode status, string responseBody = "{}")
        {
            _status = status;
            _responseBody = responseBody;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            RequestBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(_status) { Content = new StringContent(_responseBody) };
        }
    }

    private static ResendEmailSender CreateSender(StubHandler handler)
    {
        var settings = new EmailSettings
        {
            Enabled = true,
            ApiKey = "re_test_123",
            FromAddress = "onboarding@resend.dev",
            FromName = "Portal Jotanunes"
        };

        return new ResendEmailSender(new HttpClient(handler), Options.Create(settings));
    }

    private static EmailMessage Message()
    {
        return new EmailMessage(new[] { "a@x.com", "b@x.com" }, "Assunto", "<p>Olá</p>", "Olá");
    }

    [Fact]
    public async Task Should_Post_To_Resend_With_Bearer_And_Payload()
    {
        var handler = new StubHandler(HttpStatusCode.OK);

        await CreateSender(handler).SendAsync(Message());

        Assert.Equal(HttpMethod.Post, handler.Request!.Method);
        Assert.Equal("https://api.resend.com/emails", handler.Request.RequestUri!.ToString());
        Assert.Equal("Bearer", handler.Request.Headers.Authorization!.Scheme);
        Assert.Equal("re_test_123", handler.Request.Headers.Authorization.Parameter);

        using var json = JsonDocument.Parse(handler.RequestBody!);
        var root = json.RootElement;
        Assert.Equal("Portal Jotanunes <onboarding@resend.dev>", root.GetProperty("from").GetString());
        Assert.Equal("Assunto", root.GetProperty("subject").GetString());
        Assert.Equal("<p>Olá</p>", root.GetProperty("html").GetString());
        Assert.Equal("Olá", root.GetProperty("text").GetString());
        Assert.Equal(new[] { "a@x.com", "b@x.com" }, root.GetProperty("to").EnumerateArray().Select(e => e.GetString()));
    }

    [Fact]
    public async Task Should_Throw_With_Provider_Body_When_Resend_Returns_Error()
    {
        var handler = new StubHandler(HttpStatusCode.UnprocessableEntity, "{\"message\":\"domain not verified\"}");

        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => CreateSender(handler).SendAsync(Message()));

        Assert.Contains("422", ex.Message);
        Assert.Contains("domain not verified", ex.Message);
    }
}
