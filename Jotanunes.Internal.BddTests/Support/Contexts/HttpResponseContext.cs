using System.Net.Http.Json;

using Jotanunes.Internal.BddTests.Support.Models;

namespace Jotanunes.Internal.BddTests.Support.Contexts;

internal class HttpResponseContext(ExceptionContext exceptionContext)
{
    public HttpResponseMessage? Response { get; set; }

    public Exception? ThrownException
    {
        get { return exceptionContext.ThrownException; }
        set { exceptionContext.ThrownException = value; }
    }

    public async Task<T?> TryReadFromJsonAsync<T>()
    {
        if (Response == null) return default;
        if (Response?.Content == null) return default;
        try
        {
            Response.EnsureSuccessStatusCode();
            return await Response.Content.ReadFromJsonAsync<T>();
        }
        catch (Exception ex)
        {
            ThrownException = ex;
            return default;
        }
    }

    public async Task<ErrorResponseDto?> TryReadErrorAsync()
    {
        if (Response == null) return default;
        if (Response?.Content == null) return default;
        if (Response.IsSuccessStatusCode) return default;
        try
        {
            return await Response.Content.ReadFromJsonAsync<ErrorResponseDto>();
        }
        catch (Exception)
        {
            return default;
        }
    }
}
