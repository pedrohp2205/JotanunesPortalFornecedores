using System.Text.Json;
using Jotanunes.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Jotanunes.API.Shared.Middlewares;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        if (exception is not (JotanunesException or UnauthorizedAccessException or KeyNotFoundException))
        {
            _logger.LogError(exception, "Erro não tratado ao processar {Method} {Path}", context.Request.Method, context.Request.Path);
        }

        var response = context.Response;
        response.ContentType = "application/json";

        var (statusCode, message) = exception switch
        {
            JotanunesException ex => (StatusCodes.Status400BadRequest, ex.Message),
            UnauthorizedAccessException ex => (StatusCodes.Status401Unauthorized, ex.Message),
            KeyNotFoundException ex => (StatusCodes.Status404NotFound, ex.Message),
            _ => (StatusCodes.Status500InternalServerError, "Ocorreu um erro no sistema!")
        };

        response.StatusCode = statusCode;

        var errorResponse = new
        {
            Message = message
        };

        var result = JsonSerializer.Serialize(errorResponse);
        await response.WriteAsync(result);
    }
}
