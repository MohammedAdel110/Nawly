using Nawly.Shared.Common.Pagination;
using Nawly.Shared.Common.Params.Finance;
using Nawly.Shared.Common.Results;
using Nawly.Shared.DTOs.Courier;
using Nawly.Shared.DTOs.Finance;
using Nawly.Shared.EnumHelper.FinanceEnums;

namespace Nawly.Abstraction.IServices.Finance;

public interface IFinanceService
{
    Task RecordOrderCompletedLedgerAsync(Guid orderId, CancellationToken ct = default);
    Task RecordRefundLedgerAsync(Guid paymentId, decimal amount, string reason, CancellationToken ct = default);
    Task<Result<PaginationResponse<SettlementDto>>> GetSettlementsAsync(SettlementQueryParams queryParams, CancellationToken ct = default);
    Task<Result<SettlementDto>> GetSettlementByIdAsync(Guid settlementId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<SettlementDto>>> GenerateSettlementsAsync(Guid adminUserId, GenerateSettlementsRequestDto request, CancellationToken ct = default);
    Task<Result<SettlementDto>> ApproveSettlementAsync(Guid adminUserId, Guid settlementId, CancellationToken ct = default);
    Task<Result<SettlementDto>> MarkSettlementPaidAsync(Guid adminUserId, Guid settlementId, MarkSettlementPaidDto request, CancellationToken ct = default);
    Task<Result<IReadOnlyList<CourierCashSummaryDto>>> GetCourierCashSummariesAsync(CancellationToken ct = default);
    Task<Result> ReconcileCourierCashAsync(Guid adminUserId, ReconcileCourierCashDto request, CancellationToken ct = default);
    Task<Result<IReadOnlyList<LedgerEntryDto>>> GetLedgerEntriesAsync(LedgerAccountType? accountType, Guid? accountId, CancellationToken ct = default);
}
