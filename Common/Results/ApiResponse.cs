namespace skillexa_backend.Common.Results;

public sealed record ApiResponse<T>(bool Success, T? Data, string? Message = null, object? Errors = null)
{
    public static ApiResponse<T> Ok(T data, string? message = null) => new(true, data, message);

    public static ApiResponse<T> Fail(string message, object? errors = null) => new(false, default, message, errors);
}
