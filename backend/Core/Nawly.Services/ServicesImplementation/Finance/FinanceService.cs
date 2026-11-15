using Nawly.Abstraction.IServices.Auth;
using Nawly.Abstraction.IServices.Finance;
using Nawly.Abstraction.IServices.Pricing;
using Nawly.Domain.Contracts.SpecificReposPattern;
using Nawly.Domain.Contracts.UnitOfWorkPattern;
using Nawly.Domain.Models.Catalog;
using Nawly.Domain.Models.Finance;
using Nawly.Domain.Models.Identity;
using Nawly.Domain.Models.Payments;
using Nawly.Services.Mapping.FinanceMapping;
using Nawly.Services.Specifications.CourierSpec;
using Nawly.Services.Specifications.FinanceSpec;
using Nawly.Services.Specifications.OrderSpec;
using Nawly.Services.Specifications.PaymentSpec;
using Nawly.Services.Specifications.PromotionSpec;
using Nawly.Shared.Common.Pagination;
using Nawly.Shared.Common.Params.Courier;
using Nawly.Shared.Common.Params.Finance;
using Nawly.Shared.Common.Results;
using Nawly.Shared.DTOs.Courier;
using Nawly.Shared.DTOs.Finance;
using Nawly.Shared.EnumHelper.FinanceEnums;
using Nawly.Shared.EnumHelper.PaymentEnums;

namespace Nawly.Services.ServicesImplementation.Finance;

public sealed class FinanceService(
    IUnitOfWork unitOfWork,
    ILedgerRepository ledgerRepository,
    IDeliveryRepository deliveryRepository,
    IOrderRepository orderRepository,
    ICommissionPolicy commissionPolicy,
    IClock clock) : IFinanceService
{
    private const decimal DefaultCommissionRate = 0.12m;

    public async Task RecordOrderCompletedLedgerAsync(Guid orderId, CancellationToken ct = default)
    {
        var existing = await ledgerRepository.GetAllWithSpecAsync(new LedgerEntriesForOrderSpecification(orderId), ct);
        if (existing.Any(e => e.Kind == LedgerEntryKind.OrderRevenue))
            return;

        var order = await orderRepository.GetByIdWithSpecAsync(new OrderWithDetailsSpecification(orderId, asNoTracking: true), ct);
        if (order is null)
            return;

        var defaultRate = await ServiceHelpers.GetDecimalSettingAsync(unitOfWork, "Finance:DefaultCommissionRate", DefaultCommissionRate, ct);
        var commission = order.CommissionAmount > 0m
            ? order.CommissionAmount
            : commissionPolicy.CalculateCommission(order.Subtotal, 0m, order.Store?.CommissionRate, defaultRate);

        var entries = new List<LedgerEntry>
        {
            new()
            {
                AccountType = LedgerAccountType.Store,
                AccountId = order.StoreId,
                OrderId = order.Id,
                Kind = LedgerEntryKind.OrderRevenue,
                Direction = LedgerDirection.Credit,
                Amount = order.Subtotal,
                Description = $"Order {order.OrderNumber} store revenue",
                CreatedAt = clock.UtcNow
            },
            new()
            {
                AccountType = LedgerAccountType.Store,
                AccountId = order.StoreId,
                OrderId = order.Id,
                Kind = LedgerEntryKind.Commission,
                Direction = LedgerDirection.Debit,
                Amount = commission,
                Description = $"Platform commission on {order.OrderNumber}",
                CreatedAt = clock.UtcNow
            },
            new()
            {
                AccountType = LedgerAccountType.Platform,
                AccountId = null,
                OrderId = order.Id,
                Kind = LedgerEntryKind.Commission,
                Direction = LedgerDirection.Credit,
                Amount = commission,
                Description = $"Commission earned on {order.OrderNumber}",
                CreatedAt = clock.UtcNow
            }
        };

        var delivery = await deliveryRepository.GetByOrderIdAsync(order.Id, ct);
        if (delivery?.CourierId is Guid courierId)
        {
            entries.Add(new LedgerEntry
            {
                AccountType = LedgerAccountType.Courier,
                AccountId = courierId,
                OrderId = order.Id,
                DeliveryId = delivery.Id,
                Kind = LedgerEntryKind.CourierPayout,
                Direction = LedgerDirection.Credit,
                Amount = delivery.CourierPayout,
                Description = $"Delivery payout for {order.OrderNumber}",
                CreatedAt = clock.UtcNow
            });

            if (order.PaymentMethod == PaymentMethod.Cash)
            {
                var collected = delivery.CodCollectedAmount ?? order.Total;
                entries.Add(new LedgerEntry
                {
                    AccountType = LedgerAccountType.Courier,
                    AccountId = courierId,
                    OrderId = order.Id,
                    DeliveryId = delivery.Id,
                    Kind = LedgerEntryKind.CashCollected,
                    Direction = LedgerDirection.Debit,
                    Amount = collected,
                    Description = $"COD collected for {order.OrderNumber}",
                    CreatedAt = clock.UtcNow
                });
            }
        }

        await ledgerRepository.AddRangeAsync(entries, ct);
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task RecordRefundLedgerAsync(Guid paymentId, decimal amount, string reason, CancellationToken ct = default)
    {
        var payment = await unitOfWork
            .GetRepository<Domain.Models.Payments.Payment, Guid>()
            .GetByIdWithSpecAsync(new PaymentWithRefundsSpecification(paymentId, asNoTracking: true), ct);

        if (payment is null)
            return;

        await ledgerRepository.AddAsync(new LedgerEntry
        {
            AccountType = LedgerAccountType.Platform,
            AccountId = null,
            OrderId = payment.OrderId,
            PaymentId = payment.Id,
            Kind = LedgerEntryKind.Refund,
            Direction = LedgerDirection.Debit,
            Amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero),
            Description = $"Refund ({reason})",
            CreatedAt = clock.UtcNow
        }, ct);

        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task<Result<PaginationResponse<SettlementDto>>> GetSettlementsAsync(
        SettlementQueryParams queryParams,
        CancellationToken ct = default)
    {
        var repo = unitOfWork.GetRepository<Settlement, Guid>();
        var items = await repo.GetAllWithSpecAsync(new SettlementsFilterSpecification(queryParams), ct);
        var count = await repo.GetCountAsync(new SettlementsCountSpecification(queryParams), ct);

        return Result.Success(new PaginationResponse<SettlementDto>(
            queryParams.PageIndex,
            queryParams.PageSize,
            count,
            items.Select(s => s.ToDto()).ToList()));
    }

    public async Task<Result<SettlementDto>> GetSettlementByIdAsync(Guid settlementId, CancellationToken ct = default)
    {
        var settlement = await unitOfWork
            .GetRepository<Settlement, Guid>()
            .GetByIdWithSpecAsync(new SettlementWithLinesSpecification(settlementId, asNoTracking: true), ct);

        if (settlement is null)
            return Result.Failure<SettlementDto>(Error.NotFound("finance.settlement_not_found", "Settlement not found."));

        return Result.Success(settlement.ToDto());
    }

    public async Task<Result<IReadOnlyList<SettlementDto>>> GenerateSettlementsAsync(
        Guid adminUserId,
        GenerateSettlementsRequestDto request,
        CancellationToken ct = default)
    {
        if (request.PeriodEnd <= request.PeriodStart)
            return Result.Failure<IReadOnlyList<SettlementDto>>(Error.Validation("finance.invalid_period", "PeriodEnd must be after PeriodStart."));

        var created = new List<SettlementDto>();
        var settlementRepo = unitOfWork.GetRepository<Settlement, Guid>();

        if (request.PartyType is null or SettlementPartyType.Store)
        {
            var storeIds = request.PartyId.HasValue
                ? [request.PartyId.Value]
                : (await unitOfWork.GetRepository<Domain.Models.Catalog.Store, Guid>().GetAllAsync(ct)).Where(s => !s.IsDeleted).Select(s => s.Id).ToList();

            foreach (var storeId in storeIds)
            {
                var unsettled = await ledgerRepository.GetUnsettledEntriesAsync(
                    LedgerAccountType.Store, storeId, request.PeriodStart, request.PeriodEnd, ct);

                if (unsettled.Count == 0) continue;

                var gross = unsettled.Where(e => e.Kind == LedgerEntryKind.OrderRevenue).Sum(e => e.Amount);
                var commission = unsettled.Where(e => e.Kind == LedgerEntryKind.Commission).Sum(e => e.Amount);
                var adjustments = unsettled.Where(e => e.Kind is LedgerEntryKind.Adjustment or LedgerEntryKind.Refund or LedgerEntryKind.Discount)
                    .Sum(e => e.Direction == LedgerDirection.Credit ? e.Amount : -e.Amount);
                var net = decimal.Round(gross - commission + adjustments, 2, MidpointRounding.AwayFromZero);

                var settlement = BuildSettlement(SettlementPartyType.Store, storeId, request, gross, commission, adjustments, net, unsettled, adminUserId);
                await settlementRepo.AddAsync(settlement, ct);
                MarkEntriesSettled(unsettled, settlement.Id);
                created.Add(settlement.ToDto());
            }
        }

        if (request.PartyType is null or SettlementPartyType.Courier)
        {
            var courierIds = request.PartyId.HasValue
                ? [request.PartyId.Value]
                : (await unitOfWork.GetRepository<CourierProfile, Guid>().GetAllAsync(ct)).Where(c => !c.IsDeleted).Select(c => c.Id).ToList();

            foreach (var courierId in courierIds)
            {
                var unsettled = await ledgerRepository.GetUnsettledEntriesAsync(
                    LedgerAccountType.Courier, courierId, request.PeriodStart, request.PeriodEnd, ct);

                if (unsettled.Count == 0) continue;

                var gross = unsettled.Where(e => e.Kind == LedgerEntryKind.CourierPayout).Sum(e => e.Amount);
                var adjustments = unsettled.Where(e => e.Kind != LedgerEntryKind.CourierPayout)
                    .Sum(e => e.Direction == LedgerDirection.Credit ? e.Amount : -e.Amount);
                var net = decimal.Round(gross + adjustments, 2, MidpointRounding.AwayFromZero);

                var settlement = BuildSettlement(SettlementPartyType.Courier, courierId, request, gross, 0m, adjustments, net, unsettled, adminUserId);
                await settlementRepo.AddAsync(settlement, ct);
                MarkEntriesSettled(unsettled, settlement.Id);
                created.Add(settlement.ToDto());
            }
        }

        if (created.Count > 0)
            await unitOfWork.SaveChangesAsync(ct);

        return Result.Success<IReadOnlyList<SettlementDto>>(created);
    }

    public async Task<Result<SettlementDto>> ApproveSettlementAsync(Guid adminUserId, Guid settlementId, CancellationToken ct = default)
    {
        var repo = unitOfWork.GetRepository<Settlement, Guid>();
        var settlement = await repo.GetByIdWithSpecAsync(new SettlementWithLinesSpecification(settlementId), ct);
        if (settlement is null)
            return Result.Failure<SettlementDto>(Error.NotFound("finance.settlement_not_found", "Settlement not found."));

        var result = settlement.Approve(adminUserId, clock.UtcNow);
        if (result.IsFailure)
            return Result.Failure<SettlementDto>(result.Error);

        repo.Update(settlement);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(settlement.ToDto());
    }

    public async Task<Result<SettlementDto>> MarkSettlementPaidAsync(
        Guid adminUserId,
        Guid settlementId,
        MarkSettlementPaidDto request,
        CancellationToken ct = default)
    {
        var repo = unitOfWork.GetRepository<Settlement, Guid>();
        var settlement = await repo.GetByIdWithSpecAsync(new SettlementWithLinesSpecification(settlementId), ct);
        if (settlement is null)
            return Result.Failure<SettlementDto>(Error.NotFound("finance.settlement_not_found", "Settlement not found."));

        var result = settlement.MarkPaid(request.PaymentReference, clock.UtcNow);
        if (result.IsFailure)
            return Result.Failure<SettlementDto>(result.Error);

        settlement.UpdatedBy = adminUserId.ToString();
        repo.Update(settlement);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(settlement.ToDto());
    }

    public async Task<Result<IReadOnlyList<CourierCashSummaryDto>>> GetCourierCashSummariesAsync(CancellationToken ct = default)
    {
        var couriers = await unitOfWork
            .GetRepository<CourierProfile, Guid>()
            .GetAllWithSpecAsync(new CouriersFilterSpecification(new CourierQueryParams { PageSize = 50 }), ct);

        var summaries = couriers
            .Where(c => c.CashOnHand > 0m)
            .Select(c => new CourierCashSummaryDto(c.Id, c.User?.FullName ?? string.Empty, c.CashOnHand, 0, null))
            .ToList();

        return Result.Success<IReadOnlyList<CourierCashSummaryDto>>(summaries);
    }

    public async Task<Result> ReconcileCourierCashAsync(
        Guid adminUserId,
        ReconcileCourierCashDto request,
        CancellationToken ct = default)
    {
        if (request.AmountReceived <= 0m)
            return Result.Failure(Error.Validation("finance.invalid_amount", "Amount received must be greater than zero."));

        var courierRepo = unitOfWork.GetRepository<CourierProfile, Guid>();
        var courier = await courierRepo.GetByIdAsync(request.CourierId, ct);
        if (courier is null || courier.IsDeleted)
            return Result.Failure(Error.NotFound("courier.not_found", "Courier profile not found."));

        var amount = decimal.Round(request.AmountReceived, 2, MidpointRounding.AwayFromZero);
        courier.DeductCashOnHand(amount);
        courier.UpdatedAt = clock.UtcNow;
        courier.UpdatedBy = adminUserId.ToString();
        courierRepo.Update(courier);

        await ledgerRepository.AddAsync(new LedgerEntry
        {
            AccountType = LedgerAccountType.Courier,
            AccountId = courier.Id,
            Kind = LedgerEntryKind.Settlement,
            Direction = LedgerDirection.Credit,
            Amount = amount,
            Description = $"Cash reconciliation receipt {request.ReceiptReference}" + (string.IsNullOrWhiteSpace(request.Note) ? string.Empty : $" - {request.Note}"),
            CreatedAt = clock.UtcNow,
            CreatedBy = adminUserId.ToString()
        }, ct);

        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<LedgerEntryDto>>> GetLedgerEntriesAsync(
        LedgerAccountType? accountType,
        Guid? accountId,
        CancellationToken ct = default)
    {
        var entries = await ledgerRepository.GetAllWithSpecAsync(new LedgerEntriesByAccountSpecification(accountType, accountId), ct);
        return Result.Success<IReadOnlyList<LedgerEntryDto>>(entries.Select(e => e.ToDto()).ToList());
    }

    private Settlement BuildSettlement(
        SettlementPartyType partyType,
        Guid partyId,
        GenerateSettlementsRequestDto request,
        decimal gross,
        decimal commission,
        decimal adjustments,
        decimal net,
        IReadOnlyList<LedgerEntry> entries,
        Guid adminUserId)
    {
        var settlement = new Settlement
        {
            PartyType = partyType,
            PartyId = partyId,
            PeriodStart = request.PeriodStart,
            PeriodEnd = request.PeriodEnd,
            GrossAmount = decimal.Round(gross, 2, MidpointRounding.AwayFromZero),
            CommissionAmount = decimal.Round(commission, 2, MidpointRounding.AwayFromZero),
            AdjustmentsAmount = decimal.Round(adjustments, 2, MidpointRounding.AwayFromZero),
            NetAmount = net,
            CreatedAt = clock.UtcNow,
            CreatedBy = adminUserId.ToString()
        };

        foreach (var entry in entries)
        {
            settlement.Lines.Add(new SettlementLine
            {
                SettlementId = settlement.Id,
                OrderId = entry.OrderId,
                LedgerEntryId = entry.Id,
                Amount = entry.Direction == LedgerDirection.Credit ? entry.Amount : -entry.Amount
            });
        }

        return settlement;
    }

    private void MarkEntriesSettled(IEnumerable<LedgerEntry> entries, Guid settlementId)
    {
        foreach (var entry in entries)
        {
            entry.SettlementId = settlementId;
            ledgerRepository.Update(entry);
        }
    }
}
