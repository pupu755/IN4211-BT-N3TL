using System.Net;
using System.Text.Json;
using FoodHub.Application.Common;
using FoodHub.Domain.Common;

namespace FoodHub.API.Middleware;

/// <summary>Uniform JSON body for every error response.</summary>
public sealed record ErrorResponse(int Status, string Code, string Message, IDictionary<string, string[]>? Errors, string TraceId);

/// <summary>Turns exceptions into JSON errors with the right HTTP status (401, 403, 404, 409, 422, 500).</summary>
public sealed class ExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await WriteAsync(context, ex);
        }
    }

    private async Task WriteAsync(HttpContext context, Exception ex)
    {
        var (status, code, errors) = ex switch
        {
            NotFoundException => (HttpStatusCode.NotFound, "not_found", null),
            ConflictException => (HttpStatusCode.Conflict, "conflict", null),
            ForbiddenException => (HttpStatusCode.Forbidden, "forbidden", null),
            UnauthorizedAppException => (HttpStatusCode.Unauthorized, "unauthorized", null),
            RequestValidationException v => (HttpStatusCode.UnprocessableEntity, "validation_failed", v.Errors),
            DomainException => (HttpStatusCode.Conflict, "business_rule_violation", null),
            _ => (HttpStatusCode.InternalServerError, "server_error", null)
        };

        string message;
        if (status == HttpStatusCode.InternalServerError)
        {
            _logger.LogError(ex, "Unhandled exception");
            message = "An unexpected error occurred.";
        }
        else
        {
            message = ex.Message;
        }

        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = (int)status;
        context.Response.ContentType = "application/json";
        var body = new ErrorResponse((int)status, code, message, errors, context.TraceIdentifier);
        await context.Response.WriteAsync(JsonSerializer.Serialize(body, JsonOptions));
    }
}
