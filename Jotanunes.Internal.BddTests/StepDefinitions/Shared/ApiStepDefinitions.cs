using AwesomeAssertions;
using AwesomeAssertions.Execution;

using Jotanunes.Internal.BddTests.Support.Contexts;

using Microsoft.AspNetCore.Http;

namespace Jotanunes.Internal.BddTests.StepDefinitions.Shared;

[Binding]
internal class ApiStepDefinitions(HttpResponseContext httpResponseCtx)
{
    [Then(@"^eu recebo uma resposta (200 OK|201 Created|204 No Content)$")]
    public async Task EntaoEuReceboUmaRespostaComStatusDeSucesso(string status)
    {
        ArgumentNullException.ThrowIfNull(httpResponseCtx.Response);
        var expected = status switch
        {
            "200 OK" => StatusCodes.Status200OK,
            "201 Created" => StatusCodes.Status201Created,
            "204 No Content" => StatusCodes.Status204NoContent,
            _ => throw new ArgumentException($"Status '{status}' não é reconhecido")
        };

        Exception? httpResponseException = null;
        var errorMessage = string.Empty;
        try
        {
            httpResponseCtx.Response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            httpResponseException = ex;
            var error = await httpResponseCtx.TryReadErrorAsync();
            errorMessage = error?.Message ?? string.Empty;
        }

        using (new AssertionScope())
        {
            ((int)httpResponseCtx.Response.StatusCode).Should().Be(expected);
            errorMessage.Should().BeNullOrEmpty();
            httpResponseException.Should().BeNull();
            httpResponseException?.Message.Should().BeNull();
        }
    }

    [Then(@"^eu recebo uma resposta (400 Bad Request|401 Unauthorized|403 Forbidden|404 Not Found)$")]
    public void EntaoEuReceboUmaRespostaComStatusDeErro(string status)
    {
        ArgumentNullException.ThrowIfNull(httpResponseCtx.Response);
        var expected = status switch
        {
            "400 Bad Request" => StatusCodes.Status400BadRequest,
            "401 Unauthorized" => StatusCodes.Status401Unauthorized,
            "403 Forbidden" => StatusCodes.Status403Forbidden,
            "404 Not Found" => StatusCodes.Status404NotFound,
            _ => throw new ArgumentException($"Status '{status}' não é reconhecido")
        };
        ((int)httpResponseCtx.Response.StatusCode).Should().Be(expected);
    }

    [Then("eu recebo uma resposta de sucesso")]
    public void EntaoEuReceboUmaRespostaDeSucesso()
    {
        ArgumentNullException.ThrowIfNull(httpResponseCtx.Response);
        Exception? httpResponseException = null;
        try { httpResponseCtx.Response?.EnsureSuccessStatusCode(); }
        catch (Exception ex) { httpResponseException = ex; }
        using (new AssertionScope())
        {
            httpResponseException.Should().BeNull();
            httpResponseException?.Message.Should().BeNull();
        }
    }

    [Then("eu recebo uma resposta de erro")]
    public void EntaoEuReceboUmaRespostaDeErro()
    {
        Assert.Throws<HttpRequestException>(() => httpResponseCtx.Response?.EnsureSuccessStatusCode());
    }

    [Then(@"eu recebo uma resposta de erro com a mensagem ""(.*)""")]
    public async Task EntaoDeveSerApresentadaAMensagemDeErro(string expectedErrorMessage)
    {
        var error = await httpResponseCtx.TryReadErrorAsync();
        error.Should().NotBeNull();
        error!.Message.Should().Be(expectedErrorMessage);
    }
}
