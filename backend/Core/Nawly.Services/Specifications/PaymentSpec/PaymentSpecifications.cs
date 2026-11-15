using Nawly.Domain.Contracts.SpecificationPattern.BaseSpec;
using Nawly.Domain.Models.Payments;
using Nawly.Shared.Common.Params.Payment;

namespace Nawly.Services.Specifications.PaymentSpec;

public sealed class PaymentByOrderIdSpecification : BaseSpecifications<Payment, Guid>
{
    public PaymentByOrderIdSpecification(Guid orderId, bool asNoTracking = true)
        : base(p => p.OrderId == orderId && !p.IsDeleted)
    {
        AddInclude(p => p.Refunds);
        AddOrderBy(p => p.InitiatedAt, isDescending: true);
        if (asNoTracking)
            ApplyAsNoTracking();
    }
}

public sealed class PaymentWithRefundsSpecification : BaseSpecifications<Payment, Guid>
{
    public PaymentWithRefundsSpecification(Guid paymentId, bool asNoTracking = false)
        : base(p => p.Id == paymentId && !p.IsDeleted)
    {
        AddInclude(p => p.Order);
        AddInclude(p => p.Refunds);
        ApplySplitQuery();
        if (asNoTracking)
            ApplyAsNoTracking();
    }
}

public sealed class PaymentsFilterSpecification : BaseSpecifications<Payment, Guid>
{
    public PaymentsFilterSpecification(PaymentQueryParams queryParams)
    {
        var orderId = queryParams.OrderId;
        var status = queryParams.Status;
        var provider = queryParams.Provider;

        Criteria = p => !p.IsDeleted
            && (orderId == null || p.OrderId == orderId)
            && (status == null || p.Status == status)
            && (provider == null || p.Provider == provider);

        AddInclude(p => p.Refunds);
        AddOrderBy(p => p.InitiatedAt, isDescending: true);
        ApplyPaging(queryParams.PageSize, queryParams.PageIndex);
        ApplyAsNoTracking();
    }
}

public sealed class PaymentsCountSpecification : BaseSpecifications<Payment, Guid>
{
    public PaymentsCountSpecification(PaymentQueryParams queryParams)
    {
        var orderId = queryParams.OrderId;
        var status = queryParams.Status;
        var provider = queryParams.Provider;

        Criteria = p => !p.IsDeleted
            && (orderId == null || p.OrderId == orderId)
            && (status == null || p.Status == status)
            && (provider == null || p.Provider == provider);
    }
}

public sealed class PaymentByIdempotencyKeySpecification : BaseSpecifications<Payment, Guid>
{
    public PaymentByIdempotencyKeySpecification(Guid orderId, Guid idempotencyKey)
        : base(p => p.OrderId == orderId && p.IdempotencyKey == idempotencyKey && !p.IsDeleted)
    {
        AddInclude(p => p.Refunds);
    }
}

public sealed class PaymentByProviderOrderIdSpecification : BaseSpecifications<Payment, Guid>
{
    public PaymentByProviderOrderIdSpecification(Nawly.Shared.EnumHelper.PaymentEnums.PaymentProvider provider, string providerOrderId)
        : base(p => p.Provider == provider && p.ProviderOrderId == providerOrderId && !p.IsDeleted)
    {
        AddInclude(p => p.Refunds);
    }
}

public sealed class WebhookEventByExternalIdSpecification : BaseSpecifications<PaymentWebhookEvent, Guid>
{
    public WebhookEventByExternalIdSpecification(Nawly.Shared.EnumHelper.PaymentEnums.PaymentProvider provider, string externalEventId)
        : base(e => e.Provider == provider && e.ExternalEventId == externalEventId && !e.IsDeleted)
    {
    }
}
