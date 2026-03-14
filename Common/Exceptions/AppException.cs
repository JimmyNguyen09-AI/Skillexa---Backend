using System.Net;

namespace skillexa_backend.Common.Exceptions;

public class AppException(string message, HttpStatusCode statusCode = HttpStatusCode.BadRequest, object? errors = null)
    : Exception(message)
{
    public HttpStatusCode StatusCode { get; } = statusCode;

    public object? Errors { get; } = errors;
}
