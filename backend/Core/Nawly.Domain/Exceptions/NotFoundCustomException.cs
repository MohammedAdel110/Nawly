namespace Nawly.Domain.Exceptions;

public class NotFoundCustomException : Exception
{
    public string Code { get; }

    public NotFoundCustomException(string message, string code = "common.not_found")
        : base(message)
    {
        Code = code;
    }
}
