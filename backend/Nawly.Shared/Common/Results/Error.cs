namespace Nawly.Shared.Common.Results;

/// <summary>
/// Stable machine-readable domain/application error mapped to RFC 9457 Problem Details (SRS §7.1).
/// </summary>
public sealed record Error(string Code, string Message, int StatusCode = 400)
{
    public static readonly Error None = new(string.Empty, string.Empty, 200);
    public static readonly Error NullValue = new("common.null_value", "A required value was null.", 400);

    public static Error Validation(string code, string message) => new(code, message, 400);
    public static Error Unauthorized(string code, string message) => new(code, message, 401);
    public static Error Forbidden(string code, string message) => new(code, message, 403);
    public static Error NotFound(string code, string message) => new(code, message, 404);
    public static Error Conflict(string code, string message) => new(code, message, 409);
    public static Error Unprocessable(string code, string message) => new(code, message, 422);
}
