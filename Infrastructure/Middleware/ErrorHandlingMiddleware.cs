using System.Net;
using System.Text.Json;
using skillexa_backend.Common.Exceptions;
using skillexa_backend.Common.Results;

namespace skillexa_backend.Infrastructure.Middleware;

public sealed class ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
{
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (AppException exception)
        {
            logger.LogWarning(exception, "Handled application error");
            await WriteErrorAsync(context, exception.StatusCode, exception.Message, exception.Errors);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled error");
            await WriteErrorAsync(context, HttpStatusCode.InternalServerError, "An unexpected error occurred.");
        }
    }

    private static async Task WriteErrorAsync(HttpContext context, HttpStatusCode statusCode, string message, object? errors = null)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var payload = ApiResponse<object>.Fail(message, errors);
        await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}
