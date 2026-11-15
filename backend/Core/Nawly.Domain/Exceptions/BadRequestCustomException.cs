namespace Nawly.Domain.Exceptions;

public class BadRequestCustomException : Exception
{
    public string Code { get; }
    public IEnumerable<string>? Errors { get; }

    public BadRequestCustomException(string message, string code = "common.bad_request", IEnumerable<string>? errors = null)
        : base(message)
    {
        Code = code;
        Errors = errors;
    }
}
