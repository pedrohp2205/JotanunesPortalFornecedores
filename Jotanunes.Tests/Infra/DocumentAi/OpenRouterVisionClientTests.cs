using System.Net;
using System.Text.Json;
using Jotanunes.Application.DTOs.Analysis;
using Jotanunes.Application.Exceptions;
using Jotanunes.Infra.DocumentAi.Services;
using Jotanunes.Infra.DocumentAi.Settings;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Jotanunes.Tests.Infra.DocumentAi;

public class OpenRouterVisionClientTests
{
    private const string Schema = """{"type":"object","properties":{"cpf":{"type":["string","null"]}},"required":["cpf"],"additionalProperties":false}""";

    private static readonly VisionRequest Request = new(
        "Transcreva o recibo.",
        "payment_receipt",
        Schema,
        [new DocumentImage([1, 2, 3], "image/png")]);

    private static (OpenRouterVisionClient Client, FakeHandler Handler) Client(HttpStatusCode status, string body)
    {
        var handler = new FakeHandler(_ => new HttpResponseMessage(status) { Content = new StringContent(body) });
        return (Build(handler), handler);
    }

    private static OpenRouterVisionClient Build(FakeHandler handler)
    {
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://openrouter.test/api/v1/") };
        var settings = Options.Create(new OpenRouterSettings { ApiKey = "chave-de-teste", Model = "google/gemini-3.8-flash" });
        return new OpenRouterVisionClient(http, settings, NullLogger<OpenRouterVisionClient>.Instance);
    }

    private static string Completion(string content) => JsonSerializer.Serialize(new
    {
        model = "google/gemini-3.8-flash",
        choices = new[] { new { message = new { content } } },
        usage = new { prompt_tokens = 1500, completion_tokens = 80, cost = 0.0014 }
    });

    [Fact]
    public async Task Should_Send_Image_Schema_And_Zero_Data_Retention()
    {
        var (client, handler) = Client(HttpStatusCode.OK, Completion("""{"cpf":"529.982.247-25"}"""));

        await client.ExtractJson(Request);

        Assert.Equal("https://openrouter.test/api/v1/chat/completions", handler.Request!.RequestUri!.ToString());
        Assert.Equal("Bearer chave-de-teste", handler.Request.Headers.Authorization!.ToString());

        using var body = JsonDocument.Parse(handler.Body!);
        var root = body.RootElement;
        Assert.Equal("google/gemini-3.8-flash", root.GetProperty("model").GetString());
        Assert.False(root.TryGetProperty("temperature", out _));
        Assert.True(root.GetProperty("provider").GetProperty("zdr").GetBoolean());
        Assert.True(root.GetProperty("provider").GetProperty("require_parameters").GetBoolean());
        Assert.Equal("json_schema", root.GetProperty("response_format").GetProperty("type").GetString());
        Assert.Equal("payment_receipt", root.GetProperty("response_format").GetProperty("json_schema").GetProperty("name").GetString());
        Assert.Equal(JsonValueKind.Object, root.GetProperty("response_format").GetProperty("json_schema").GetProperty("schema").ValueKind);
        Assert.Equal("Transcreva o recibo.", root.GetProperty("messages")[0].GetProperty("content").GetString());
        Assert.Equal(
            "data:image/png;base64,AQID",
            root.GetProperty("messages")[1].GetProperty("content")[1].GetProperty("image_url").GetProperty("url").GetString());
    }

    [Fact]
    public async Task Should_Return_The_Model_Json()
    {
        var (client, _) = Client(HttpStatusCode.OK, Completion("""{"cpf":"529.982.247-25"}"""));

        Assert.Equal("""{"cpf":"529.982.247-25"}""", await client.ExtractJson(Request));
    }

    [Fact]
    public async Task Should_Strip_Markdown_Code_Fence()
    {
        var (client, _) = Client(HttpStatusCode.OK, Completion("```json\n{\"cpf\":null}\n```"));

        Assert.Equal("""{"cpf":null}""", await client.ExtractJson(Request));
    }

    [Theory]
    [InlineData(HttpStatusCode.PaymentRequired)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.RequestTimeout)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task Should_Treat_Rate_Limit_And_Server_Errors_As_Temporary(HttpStatusCode status)
    {
        var (client, _) = Client(status, """{"error":{"message":"tente mais tarde"}}""");

        await Assert.ThrowsAsync<TransientAnalysisException>(() => client.ExtractJson(Request));
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.NotFound)]
    public async Task Should_Treat_Request_Errors_As_Permanent(HttpStatusCode status)
    {
        var (client, _) = Client(status, """{"error":{"message":"falhou"}}""");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => client.ExtractJson(Request));
        Assert.Contains(((int)status).ToString(), ex.Message);
    }

    [Fact]
    public async Task Should_Report_Error_Message_And_Failed_Routing_Step()
    {
        var (client, _) = Client(
            HttpStatusCode.NotFound,
            """{"error":{"message":"No endpoints found that can handle the requested parameters.","code":404,"metadata":{"failed_routing_step":"Filter by Parameters"}}}""");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => client.ExtractJson(Request));

        Assert.Equal("OpenRouter retornou 404: No endpoints found that can handle the requested parameters. (etapa do roteamento: Filter by Parameters)", ex.Message);
    }

    [Fact]
    public async Task Should_Treat_Upstream_Error_Inside_Successful_Response_By_Its_Code()
    {
        var (client, _) = Client(HttpStatusCode.OK, """{"error":{"code":502,"message":"provedor fora do ar"}}""");

        await Assert.ThrowsAsync<TransientAnalysisException>(() => client.ExtractJson(Request));
    }

    [Fact]
    public async Task Should_Treat_Timeout_As_Temporary()
    {
        var client = Build(new FakeHandler(_ => throw new TaskCanceledException("timeout")));

        await Assert.ThrowsAsync<TransientAnalysisException>(() => client.ExtractJson(Request, CancellationToken.None));
    }

    [Fact]
    public async Task Should_Treat_Network_Failure_As_Temporary()
    {
        var client = Build(new FakeHandler(_ => throw new HttpRequestException("conexão recusada")));

        await Assert.ThrowsAsync<TransientAnalysisException>(() => client.ExtractJson(Request));
    }

    private sealed class FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return respond(request);
        }
    }
}
