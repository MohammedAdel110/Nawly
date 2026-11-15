namespace Nawly.Domain.Exceptions;

public class ConflictCustomException : Exception
{
    public string Code { get; }

    public ConflictCustomException(string message, string code = "common.conflict")
        : base(message)
    {
        Code = code;
    }
}
