using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Jotanunes.Application.DTOs.Analysis;
using Jotanunes.Application.Exceptions;
using Jotanunes.Application.Interfaces;
using Jotanunes.Infra.DocumentAi.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jotanunes.Infra.DocumentAi.Services;

public class OpenRouterVisionClient(
    HttpClient httpClient,
    IOptions<OpenRouterSettings> settings,
    ILogger<OpenRouterVisionClient> logger) : IVisionClient
{
    private const int MaxErrorBodyLength = 300;

    private readonly OpenRouterSettings _settings = settings.Value;

    public async Task<string> ExtractJson(VisionRequest request, CancellationToken cancellationToken = default)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
        {
            Content = JsonContent.Create(BuildBody(request))
        };
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);
        message.Headers.Add("X-Title", "Jotanunes Portal do Fornecedor");

        HttpResponseMessage response;
        try
        {
            response = await httpClient.SendAsync(message, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new TransientAnalysisException("OpenRouter indisponível.", ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TransientAnalysisException("OpenRouter não respondeu dentro do tempo limite.", ex);
        }

        using (response)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw Failure((int)response.StatusCode, body);
            }

            return ReadContent(body, request.SchemaName);
        }
    }

    private object BuildBody(VisionRequest request)
    {
        var userContent = new List<object> { new { type = "text", text = "Extraia os campos deste documento." } };
        userContent.AddRange(request.Images.Select(image => new
        {
            type = "image_url",
            image_url = new { url = $"data:{image.ContentType};base64,{Convert.ToBase64String(image.Content)}" }
        }));

        return new
        {
            model = _settings.Model,
            messages = new object[]
            {
                new { role = "system", content = request.Instructions },
                new { role = "user", content = userContent }
            },
            response_format = new
            {
                type = "json_schema",
                json_schema = new { name = request.SchemaName, strict = true, schema = JsonNode.Parse(request.JsonSchema) }
            },
            provider = new { zdr = true, require_parameters = true }
        };
    }

    private string ReadContent(string body, string schemaName)
    {
        using var json = JsonDocument.Parse(body);
        var root = json.RootElement;

        if (root.TryGetProperty("error", out var error))
        {
            var code = error.TryGetProperty("code", out var c) && c.TryGetInt32(out var parsed) ? parsed : 0;
            throw Failure(code, error.GetRawText());
        }

        var content = root.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new TransientAnalysisException("OpenRouter retornou uma resposta vazia.");
        }

        if (root.TryGetProperty("usage", out var usage))
        {
            logger.LogInformation(
                "Leitura por imagem ({Schema}) com {Model}: {PromptTokens} tokens de entrada, {CompletionTokens} de saída, custo {Cost}.",
                schemaName,
                root.TryGetProperty("model", out var model) ? model.GetString() : _settings.Model,
                usage.TryGetProperty("prompt_tokens", out var prompt) ? prompt.GetInt32() : 0,
                usage.TryGetProperty("completion_tokens", out var completion) ? completion.GetInt32() : 0,
                usage.TryGetProperty("cost", out var cost) ? cost.GetRawText() : "n/d");
        }

        return StripCodeFence(content);
    }

    private static Exception Failure(int statusCode, string body)
    {
        var detail = ErrorDetail(body);

        return statusCode is (int)HttpStatusCode.PaymentRequired or (int)HttpStatusCode.TooManyRequests or (int)HttpStatusCode.RequestTimeout or >= 500
            ? new TransientAnalysisException($"OpenRouter retornou {statusCode}: {detail}")
            : new InvalidOperationException($"OpenRouter retornou {statusCode}: {detail}");
    }

    private static string ErrorDetail(string body)
    {
        try
        {
            using var json = JsonDocument.Parse(body);
            var error = json.RootElement.TryGetProperty("error", out var e) ? e : json.RootElement;
            var message = error.TryGetProperty("message", out var m) ? m.GetString() : null;
            var step = error.TryGetProperty("metadata", out var metadata) && metadata.TryGetProperty("failed_routing_step", out var s)
                ? s.GetString()
                : null;

            if (message is not null)
            {
                return step is null ? message : $"{message} (etapa do roteamento: {step})";
            }
        }
        catch (JsonException)
        {
        }

        return body.Length <= MaxErrorBodyLength ? body : body[..MaxErrorBodyLength];
    }

    private static string StripCodeFence(string content)
    {
        var trimmed = content.Trim();
        if (!trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            return trimmed;
        }

        var start = trimmed.IndexOf('\n');
        var end = trimmed.LastIndexOf("```", StringComparison.Ordinal);
        return start >= 0 && end > start ? trimmed[(start + 1)..end].Trim() : trimmed;
    }
}
