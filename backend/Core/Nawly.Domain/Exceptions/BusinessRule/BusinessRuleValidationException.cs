namespace Nawly.Domain.Exceptions.BusinessRule;

/// <summary>
/// Maps to HTTP 422 Unprocessable Entity per SRS §7.1.
/// </summary>
public class BusinessRuleValidationException : Exception
{
    public string Code { get; }

    public BusinessRuleValidationException(string message, string code = "business.rule_violation")
        : base(message)
    {
        Code = code;
    }
}
