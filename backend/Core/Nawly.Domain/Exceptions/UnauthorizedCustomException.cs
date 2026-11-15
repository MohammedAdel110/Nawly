namespace Nawly.Domain.Exceptions;

public class UnauthorizedCustomException : Exception
{
    public string Code { get; }

    public UnauthorizedCustomException(string message = "Authentication is required.", string code = "auth.unauthorized")
        : base(message)
    {
        Code = code;
    }
}
