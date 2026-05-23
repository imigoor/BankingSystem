using Clients.Domain.Exceptions;
using FluentValidation;
using System.Net;
using System.Text.Json;

namespace Clients.API.Middlewares;

public sealed class ExceptionHandlingMiddleware
{
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
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);

        var (statusCode, title, detail) = exception switch
        {
            ValidationException ve => (
                HttpStatusCode.BadRequest,
                "Validation Failed",
                string.Join("; ", ve.Errors.Select(e => e.ErrorMessage))),
            DomainException => (HttpStatusCode.UnprocessableEntity, "Domain Rule Violation", exception.Message),
            ClientNotFoundException => (HttpStatusCode.NotFound, "Resource Not Found", exception.Message),
            InvalidOperationException => (HttpStatusCode.BadRequest, "Invalid Operation", exception.Message),
            _ => (HttpStatusCode.InternalServerError, "Internal Server Error", "An unexpected error occurred.")
        };

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/problem+json";

        var problem = new
        {
            type = $"https://httpstatuses.com/{(int)statusCode}",
            title,
            status = (int)statusCode,
            detail,
            traceId = context.TraceIdentifier
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
    }
}
