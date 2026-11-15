namespace Nawly.Domain.ValueObjects;

/// <summary>
/// Monetary value object fixed to EGP with 2-decimal MidpointRounding.AwayFromZero (SRS §5.4, §6.1).
/// </summary>
public sealed record Money
{
    public const string DefaultCurrency = "EGP";

    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency = DefaultCurrency)
    {
        Amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
        Currency = string.IsNullOrWhiteSpace(currency) ? DefaultCurrency : currency.Trim().ToUpperInvariant();
    }

    public static Money Zero(string currency = DefaultCurrency) => new(0m, currency);
    public static Money Egp(decimal amount) => new(amount, DefaultCurrency);

    public Money Round() => new(decimal.Round(Amount, 2, MidpointRounding.AwayFromZero), Currency);

    /// <summary>
    /// Optional COD cash rounding to nearest 0.25 or 0.50 EGP (SRS §5.4).
    /// </summary>
    public Money RoundCashTo(decimal step)
    {
        if (step <= 0m) return this;
        var rounded = decimal.Round(Amount / step, 0, MidpointRounding.AwayFromZero) * step;
        return new Money(rounded, Currency);
    }

    public Money RoundCashToQuarter() => RoundCashTo(0.25m);

    public static Money operator +(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return new Money(left.Amount + right.Amount, left.Currency);
    }

    public static Money operator -(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return new Money(left.Amount - right.Amount, left.Currency);
    }

    public static Money operator *(Money money, decimal factor)
        => new(money.Amount * factor, money.Currency);

    public static Money operator *(decimal factor, Money money)
        => money * factor;

    private static void EnsureSameCurrency(Money left, Money right)
    {
        if (!string.Equals(left.Currency, right.Currency, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Currency mismatch: '{left.Currency}' vs '{right.Currency}'.");
    }
}
