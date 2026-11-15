using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Nawly.Abstraction.IServices.Payment;
using Nawly.Shared.Common.Results;
using Nawly.Shared.EnumHelper.PaymentEnums;

namespace Nawly.Persistence.Adapters;

/// <summary>
/// Deterministic sandbox payment gateway for local development and testing when live Paymob/Fawry credentials are absent.
/// </summary>
public sealed class SandboxPaymentGateway(IConfiguration configuration) : IPaymentGateway
{
    public Task<Result<GatewayIntentionResult>> CreateIntentionAsync(
        GatewayIntentionRequest request,
        CancellationToken ct = default)
    {
        var providerOrderId = $"sbx_ord_{request.PaymentId:N}";
        var baseRedirect = configuration["Payments:SandboxCheckoutBaseUrl"]?.TrimEnd('/')
            ?? "https://sandbox.nawly.local/pay";

        var redirectUrl = $"{baseRedirect}/{providerOrderId}?order={Uri.EscapeDataString(request.OrderNumber)}&amount={request.Amount:0.00}";
        var clientSecret = $"sbx_sec_{Guid.NewGuid():N}";
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(30);

        return Task.FromResult(Result.Success(new GatewayIntentionResult(
            providerOrderId,
            redirectUrl,
            clientSecret,
            expiresAt)));
    }

    public bool VerifyWebhookSignature(string rawPayload, IReadOnlyDictionary<string, string> headers)
    {
        var configuredSecret = configuration["Payments:WebhookSecret"];
        if (string.IsNullOrWhiteSpace(configuredSecret))
            return true;

        var headerEntry = headers.FirstOrDefault(h =>
            h.Key.Contains("signature", StringComparison.OrdinalIgnoreCase) ||
            h.Key.Equals("hmac", StringComparison.OrdinalIgnoreCase));

        if (string.IsNullOrWhiteSpace(headerEntry.Value))
            return false;

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(configuredSecret));
        var computed = Convert.ToHexStringLower(hmac.ComputeHash(Encoding.UTF8.GetBytes(rawPayload)));
        var provided = headerEntry.Value.Trim().ToLowerInvariant();

        if (computed.Length != provided.Length)
            return false;

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computed),
            Encoding.UTF8.GetBytes(provided));
    }

    public Task<Result<GatewayTransactionStatus>> GetTransactionStatusAsync(
        string providerOrderId,
        CancellationToken ct = default)
        => Task.FromResult(Result.Success(new GatewayTransactionStatus(
            $"sbx_txn_{providerOrderId}",
            providerOrderId,
            PaymentStatus.Captured,
            0m)));

    public Task<Result<GatewayRefundResult>> RefundAsync(
        string providerTransactionId,
        decimal amount,
        string reason,
        CancellationToken ct = default)
        => Task.FromResult(Result.Success(new GatewayRefundResult(
            $"sbx_ref_{Guid.NewGuid():N}",
            RefundStatus.Completed)));
}
