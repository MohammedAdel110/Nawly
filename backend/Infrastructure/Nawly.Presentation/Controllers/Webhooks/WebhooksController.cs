using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nawly.Abstraction.IServices.Payment;
using Nawly.Shared.EnumHelper.PaymentEnums;

namespace Nawly.Presentation.Controllers.Webhooks;

[AllowAnonymous]
[Route("api/v1/webhooks")]
public sealed class WebhooksController(IPaymentService paymentService) : ApiBaseController
{
    [HttpPost("payments/{provider}")]
    public async Task<IActionResult> ProcessPaymentWebhook([FromRoute] PaymentProvider provider, CancellationToken ct)
    {
        using var reader = new StreamReader(Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        var rawPayload = await reader.ReadToEndAsync(ct);

        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var header in Request.Headers)
            headers[header.Key] = header.Value.ToString();

        var result = await paymentService.ProcessWebhookAsync(provider, rawPayload, headers, ct);
        return HandleResult(result);
    }
}
