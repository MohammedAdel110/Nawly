using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nawly.Abstraction.IServices.Admin;
using Nawly.Abstraction.IServices.Courier;
using Nawly.Abstraction.IServices.Delivery;
using Nawly.Abstraction.IServices.Finance;
using Nawly.Abstraction.IServices.Geography;
using Nawly.Abstraction.IServices.Notification;
using Nawly.Abstraction.IServices.Order;
using Nawly.Abstraction.IServices.Payment;
using Nawly.Abstraction.IServices.Promotion;
using Nawly.Abstraction.IServices.Review;
using Nawly.Abstraction.IServices.Store;
using Nawly.Abstraction.IServices.Support;
using Nawly.Shared.Common.Params.Admin;
using Nawly.Shared.Common.Params.Courier;
using Nawly.Shared.Common.Params.Finance;
using Nawly.Shared.Common.Params.Order;
using Nawly.Shared.Common.Params.Payment;
using Nawly.Shared.Common.Params.Promotion;
using Nawly.Shared.Common.Params.Review;
using Nawly.Shared.Common.Params.Store;
using Nawly.Shared.Common.Params.Support;
using Nawly.Shared.DTOs.Admin;
using Nawly.Shared.DTOs.Finance;
using Nawly.Shared.DTOs.Geography;
using Nawly.Shared.DTOs.Notification;
using Nawly.Shared.DTOs.Order;
using Nawly.Shared.DTOs.Payment;
using Nawly.Shared.DTOs.Promotion;
using Nawly.Shared.DTOs.Review;
using Nawly.Shared.DTOs.Store;
using Nawly.Shared.DTOs.Support;
using Nawly.Shared.EnumHelper.FinanceEnums;

namespace Nawly.Presentation.Controllers.Admin;

[Authorize(Policy = "Admin")]
[Route("api/v1/admin")]
public sealed class AdminController(
    IAdminService adminService,
    IStoreService storeService,
    ICourierService courierService,
    IDeliveryService deliveryService,
    IOrderService orderService,
    IPaymentService paymentService,
    IGeographyService geographyService,
    IPromotionService promotionService,
    IFinanceService financeService,
    IReviewService reviewService,
    ISupportService supportService,
    INotificationService notificationService) : ApiBaseController
{
    // ==========================================
    // 1. Platform Dashboard & Users
    // ==========================================

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(CancellationToken ct)
    {
        var result = await adminService.GetDashboardAsync(ct);
        return HandleResult(result);
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers([FromQuery] UserQueryParams queryParams, CancellationToken ct)
    {
        var result = await adminService.GetUsersAsync(queryParams, ct);
        return HandleResult(result);
    }

    [HttpPost("users/{targetUserId:guid}/suspend")]
    public async Task<IActionResult> SuspendUser(
        Guid targetUserId,
        [FromBody] SuspendUserDto request,
        CancellationToken ct)
    {
        if (!TryGetAuthenticatedUserId(out var adminUserId))
            return UnauthorizedProblem();

        var result = await adminService.SuspendUserAsync(adminUserId, targetUserId, request, ct);
        return HandleResult(result);
    }

    [HttpPost("users/{targetUserId:guid}/activate")]
    public async Task<IActionResult> ActivateUser(Guid targetUserId, CancellationToken ct)
    {
        if (!TryGetAuthenticatedUserId(out var adminUserId))
            return UnauthorizedProblem();

        var result = await adminService.ActivateUserAsync(adminUserId, targetUserId, ct);
        return HandleResult(result);
    }

    // ==========================================
    // 2. Stores & Categories
    // ==========================================

    [HttpGet("stores")]
    public async Task<IActionResult> GetStores([FromQuery] StoreQueryParams queryParams, CancellationToken ct)
    {
        var result = await storeService.SearchStoresAsync(queryParams, ct);
        return HandleResult(result);
    }

    [HttpPost("stores/{storeId:guid}/review")]
    public async Task<IActionResult> ReviewStoreApplication(
        Guid storeId,
        [FromBody] ReviewApplicationDto request,
        CancellationToken ct)
    {
        if (!TryGetAuthenticatedUserId(out var adminUserId))
            return UnauthorizedProblem();

        var result = await adminService.ReviewStoreApplicationAsync(adminUserId, storeId, request, ct);
        return HandleResult(result);
    }

    [HttpPost("categories")]
    public async Task<IActionResult> CreateCategory([FromBody] UpsertStoreCategoryDto request, CancellationToken ct)
    {
        var result = await storeService.UpsertCategoryAsync(null, request, ct);
        return HandleResult(result, StatusCodes.Status201Created);
    }

    [HttpPut("categories/{categoryId:guid}")]
    public async Task<IActionResult> UpdateCategory(
        Guid categoryId,
        [FromBody] UpsertStoreCategoryDto request,
        CancellationToken ct)
    {
        var result = await storeService.UpsertCategoryAsync(categoryId, request, ct);
        return HandleResult(result);
    }

    // ==========================================
    // 3. Couriers & Manual Dispatch Override
    // ==========================================

    [HttpGet("couriers")]
    public async Task<IActionResult> GetCouriers([FromQuery] CourierQueryParams queryParams, CancellationToken ct)
    {
        var result = await courierService.SearchCouriersAsync(queryParams, ct);
        return HandleResult(result);
    }

    [HttpPost("couriers/{courierId:guid}/review")]
    public async Task<IActionResult> ReviewCourierApplication(
        Guid courierId,
        [FromBody] ReviewApplicationDto request,
        CancellationToken ct)
    {
        if (!TryGetAuthenticatedUserId(out var adminUserId))
            return UnauthorizedProblem();

        var result = await adminService.ReviewCourierApplicationAsync(adminUserId, courierId, request, ct);
        return HandleResult(result);
    }

    [HttpPost("deliveries/{deliveryId:guid}/assign")]
    public async Task<IActionResult> AssignCourierManually(
        Guid deliveryId,
        [FromBody] AssignCourierRequestDto request,
        CancellationToken ct)
    {
        if (!TryGetAuthenticatedUserId(out var adminUserId))
            return UnauthorizedProblem();

        var result = await deliveryService.AssignCourierManuallyAsync(adminUserId, deliveryId, request, ct);
        return HandleResult(result);
    }

    // ==========================================
    // 4. Orders & Payments
    // ==========================================

    [HttpGet("orders")]
    public async Task<IActionResult> GetOrders([FromQuery] OrderQueryParams queryParams, CancellationToken ct)
    {
        var result = await orderService.SearchOrdersAsync(queryParams, ct);
        return HandleResult(result);
    }

    [HttpPost("orders/{orderId:guid}/status")]
    public async Task<IActionResult> ChangeOrderStatus(
        Guid orderId,
        [FromBody] AdminChangeOrderStatusDto request,
        CancellationToken ct)
    {
        if (!TryGetAuthenticatedUserId(out var adminUserId))
            return UnauthorizedProblem();

        var result = await orderService.AdminChangeStatusAsync(adminUserId, orderId, request, ct);
        return HandleResult(result);
    }

    [HttpGet("payments")]
    public async Task<IActionResult> GetPayments([FromQuery] PaymentQueryParams queryParams, CancellationToken ct)
    {
        var result = await paymentService.SearchPaymentsAsync(queryParams, ct);
        return HandleResult(result);
    }

    [HttpPost("payments/{paymentId:guid}/refund")]
    public async Task<IActionResult> RequestRefund(
        Guid paymentId,
        [FromBody] RefundRequestDto request,
        CancellationToken ct)
    {
        if (!TryGetAuthenticatedUserId(out var adminUserId))
            return UnauthorizedProblem();

        var result = await paymentService.RequestRefundAsync(adminUserId, paymentId, request, ct);
        return HandleResult(result);
    }

    // ==========================================
    // 5. Geography (Zones & Areas)
    // ==========================================

    [HttpGet("zones")]
    public async Task<IActionResult> GetZones(CancellationToken ct)
    {
        var result = await geographyService.GetZonesAsync(activeOnly: false, ct);
        return HandleResult(result);
    }

    [HttpPost("zones")]
    public async Task<IActionResult> CreateZone([FromBody] UpsertDeliveryZoneDto request, CancellationToken ct)
    {
        var result = await geographyService.CreateZoneAsync(request, ct);
        return HandleResult(result, StatusCodes.Status201Created);
    }

    [HttpPut("zones/{zoneId:guid}")]
    public async Task<IActionResult> UpdateZone(
        Guid zoneId,
        [FromBody] UpsertDeliveryZoneDto request,
        CancellationToken ct)
    {
        var result = await geographyService.UpdateZoneAsync(zoneId, request, ct);
        return HandleResult(result);
    }

    [HttpGet("areas")]
    public async Task<IActionResult> GetAreas([FromQuery] Guid? zoneId, CancellationToken ct)
    {
        var result = await geographyService.GetAreasAsync(zoneId, activeOnly: false, ct);
        return HandleResult(result);
    }

    [HttpPost("areas")]
    public async Task<IActionResult> CreateArea([FromBody] UpsertAreaDto request, CancellationToken ct)
    {
        var result = await geographyService.CreateAreaAsync(request, ct);
        return HandleResult(result, StatusCodes.Status201Created);
    }

    [HttpPut("areas/{areaId:guid}")]
    public async Task<IActionResult> UpdateArea(
        Guid areaId,
        [FromBody] UpsertAreaDto request,
        CancellationToken ct)
    {
        var result = await geographyService.UpdateAreaAsync(areaId, request, ct);
        return HandleResult(result);
    }

    // ==========================================
    // 6. Promotions (Coupons & Banners)
    // ==========================================

    [HttpGet("coupons")]
    public async Task<IActionResult> GetCoupons([FromQuery] CouponQueryParams queryParams, CancellationToken ct)
    {
        var result = await promotionService.GetCouponsAsync(queryParams, ct);
        return HandleResult(result);
    }

    [HttpPost("coupons")]
    public async Task<IActionResult> CreateCoupon([FromBody] UpsertCouponDto request, CancellationToken ct)
    {
        var result = await promotionService.UpsertCouponAsync(null, request, ct);
        return HandleResult(result, StatusCodes.Status201Created);
    }

    [HttpPut("coupons/{couponId:guid}")]
    public async Task<IActionResult> UpdateCoupon(
        Guid couponId,
        [FromBody] UpsertCouponDto request,
        CancellationToken ct)
    {
        var result = await promotionService.UpsertCouponAsync(couponId, request, ct);
        return HandleResult(result);
    }

    [HttpGet("banners")]
    public async Task<IActionResult> GetBanners(CancellationToken ct)
    {
        var result = await promotionService.GetAllBannersAsync(ct);
        return HandleResult(result);
    }

    [HttpPost("banners")]
    public async Task<IActionResult> CreateBanner([FromBody] UpsertBannerDto request, CancellationToken ct)
    {
        var result = await promotionService.UpsertBannerAsync(null, request, ct);
        return HandleResult(result, StatusCodes.Status201Created);
    }

    [HttpPut("banners/{bannerId:guid}")]
    public async Task<IActionResult> UpdateBanner(
        Guid bannerId,
        [FromBody] UpsertBannerDto request,
        CancellationToken ct)
    {
        var result = await promotionService.UpsertBannerAsync(bannerId, request, ct);
        return HandleResult(result);
    }

    [HttpDelete("banners/{bannerId:guid}")]
    public async Task<IActionResult> DeleteBanner(Guid bannerId, CancellationToken ct)
    {
        var result = await promotionService.DeleteBannerAsync(bannerId, ct);
        return HandleResult(result);
    }

    // ==========================================
    // 7. Finance Ledger, Settlements & Courier COD Reconciliation
    // ==========================================

    [HttpGet("finance/ledger")]
    public async Task<IActionResult> GetLedgerEntries(
        [FromQuery] LedgerAccountType? accountType,
        [FromQuery] Guid? accountId,
        CancellationToken ct)
    {
        var result = await financeService.GetLedgerEntriesAsync(accountType, accountId, ct);
        return HandleResult(result);
    }

    [HttpGet("finance/settlements")]
    public async Task<IActionResult> GetSettlements([FromQuery] SettlementQueryParams queryParams, CancellationToken ct)
    {
        var result = await financeService.GetSettlementsAsync(queryParams, ct);
        return HandleResult(result);
    }

    [HttpGet("finance/settlements/{settlementId:guid}")]
    public async Task<IActionResult> GetSettlementById(Guid settlementId, CancellationToken ct)
    {
        var result = await financeService.GetSettlementByIdAsync(settlementId, ct);
        return HandleResult(result);
    }

    [HttpPost("finance/settlements/generate")]
    public async Task<IActionResult> GenerateSettlements(
        [FromBody] GenerateSettlementsRequestDto request,
        CancellationToken ct)
    {
        if (!TryGetAuthenticatedUserId(out var adminUserId))
            return UnauthorizedProblem();

        var result = await financeService.GenerateSettlementsAsync(adminUserId, request, ct);
        return HandleResult(result, StatusCodes.Status201Created);
    }

    [HttpPost("finance/settlements/{settlementId:guid}/approve")]
    public async Task<IActionResult> ApproveSettlement(Guid settlementId, CancellationToken ct)
    {
        if (!TryGetAuthenticatedUserId(out var adminUserId))
            return UnauthorizedProblem();

        var result = await financeService.ApproveSettlementAsync(adminUserId, settlementId, ct);
        return HandleResult(result);
    }

    [HttpPost("finance/settlements/{settlementId:guid}/mark-paid")]
    public async Task<IActionResult> MarkSettlementPaid(
        Guid settlementId,
        [FromBody] MarkSettlementPaidDto request,
        CancellationToken ct)
    {
        if (!TryGetAuthenticatedUserId(out var adminUserId))
            return UnauthorizedProblem();

        var result = await financeService.MarkSettlementPaidAsync(adminUserId, settlementId, request, ct);
        return HandleResult(result);
    }

    [HttpGet("finance/couriers/cash")]
    public async Task<IActionResult> GetCourierCashSummaries(CancellationToken ct)
    {
        var result = await financeService.GetCourierCashSummariesAsync(ct);
        return HandleResult(result);
    }

    [HttpPost("finance/couriers/reconcile")]
    public async Task<IActionResult> ReconcileCourierCash(
        [FromBody] ReconcileCourierCashDto request,
        CancellationToken ct)
    {
        if (!TryGetAuthenticatedUserId(out var adminUserId))
            return UnauthorizedProblem();

        var result = await financeService.ReconcileCourierCashAsync(adminUserId, request, ct);
        return HandleResult(result);
    }

    // ==========================================
    // 8. Reviews Moderation, Support Tickets, Broadcasts, Settings & Audit Logs
    // ==========================================

    [HttpGet("reviews")]
    public async Task<IActionResult> GetReviews([FromQuery] ReviewQueryParams queryParams, CancellationToken ct)
    {
        var result = await reviewService.GetReviewsAsync(queryParams, ct);
        return HandleResult(result);
    }

    [HttpPost("reviews/{reviewId:guid}/moderate")]
    public async Task<IActionResult> ModerateReview(
        Guid reviewId,
        [FromBody] ModerateReviewDto request,
        CancellationToken ct)
    {
        if (!TryGetAuthenticatedUserId(out var adminUserId))
            return UnauthorizedProblem();

        var result = await reviewService.ModerateReviewAsync(adminUserId, reviewId, request, ct);
        return HandleResult(result);
    }

    [HttpGet("tickets")]
    public async Task<IActionResult> GetSupportTickets([FromQuery] TicketQueryParams queryParams, CancellationToken ct)
    {
        var result = await supportService.GetAllTicketsAsync(queryParams, ct);
        return HandleResult(result);
    }

    [HttpGet("tickets/{ticketId:guid}")]
    public async Task<IActionResult> GetTicketDetails(Guid ticketId, CancellationToken ct)
    {
        if (!TryGetAuthenticatedUserId(out var adminUserId))
            return UnauthorizedProblem();

        var result = await supportService.GetTicketDetailsAsync(ticketId, adminUserId, isAdmin: true, ct);
        return HandleResult(result);
    }

    [HttpPost("tickets/{ticketId:guid}/messages")]
    public async Task<IActionResult> ReplyToTicket(
        Guid ticketId,
        [FromBody] ReplySupportTicketDto request,
        CancellationToken ct)
    {
        if (!TryGetAuthenticatedUserId(out var adminUserId))
            return UnauthorizedProblem();

        var result = await supportService.ReplyToTicketAsync(ticketId, adminUserId, isAdmin: true, request, ct);
        return HandleResult(result);
    }

    [HttpPost("notifications/broadcast")]
    public async Task<IActionResult> BroadcastNotification(
        [FromBody] BroadcastNotificationDto request,
        CancellationToken ct)
    {
        if (!TryGetAuthenticatedUserId(out var adminUserId))
            return UnauthorizedProblem();

        var result = await notificationService.BroadcastAsync(adminUserId, request, ct);
        return HandleResult(result);
    }

    [HttpGet("settings")]
    public async Task<IActionResult> GetSystemSettings(CancellationToken ct)
    {
        var result = await adminService.GetSystemSettingsAsync(ct);
        return HandleResult(result);
    }

    [HttpPut("settings")]
    public async Task<IActionResult> UpsertSystemSetting([FromBody] SystemSettingDto request, CancellationToken ct)
    {
        if (!TryGetAuthenticatedUserId(out var adminUserId))
            return UnauthorizedProblem();

        var result = await adminService.UpsertSystemSettingAsync(adminUserId, request, ct);
        return HandleResult(result);
    }

    [HttpGet("audit-logs")]
    public async Task<IActionResult> GetAuditLogs([FromQuery] AuditLogQueryParams queryParams, CancellationToken ct)
    {
        var result = await adminService.GetAuditLogsAsync(queryParams, ct);
        return HandleResult(result);
    }
}
