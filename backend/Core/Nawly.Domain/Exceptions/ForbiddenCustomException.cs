namespace Nawly.Domain.Exceptions;

public class ForbiddenCustomException : Exception
{
    public string Code { get; }

    public ForbiddenCustomException(string message = "You do not have permission to perform this action.", string code = "auth.forbidden")
        : base(message)
    {
        Code = code;
    }
}
