using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Nawly.Abstraction.IServices.Address;
using Nawly.Abstraction.IServices.Admin;
using Nawly.Abstraction.IServices.Attachment;
using Nawly.Abstraction.IServices.Auth;
using Nawly.Abstraction.IServices.Cart;
using Nawly.Abstraction.IServices.Catalog;
using Nawly.Abstraction.IServices.Courier;
using Nawly.Abstraction.IServices.Delivery;
using Nawly.Abstraction.IServices.Dispatch;
using Nawly.Abstraction.IServices.Finance;
using Nawly.Abstraction.IServices.Geography;
using Nawly.Abstraction.IServices.Notification;
using Nawly.Abstraction.IServices.Order;
using Nawly.Abstraction.IServices.Payment;
using Nawly.Abstraction.IServices.Pricing;
using Nawly.Abstraction.IServices.Profile;
using Nawly.Abstraction.IServices.Promotion;
using Nawly.Abstraction.IServices.Review;
using Nawly.Abstraction.IServices.Store;
using Nawly.Abstraction.IServices.Support;
using Nawly.Abstraction.IServices.Token;
using Nawly.Domain.Events;
using Nawly.Services.EventHandlers;
using Nawly.Services.ServicesImplementation.Address;
using Nawly.Services.ServicesImplementation.Admin;
using Nawly.Services.ServicesImplementation.Attachment;
using Nawly.Services.ServicesImplementation.Auth;
using Nawly.Services.ServicesImplementation.Cart;
using Nawly.Services.ServicesImplementation.Catalog;
using Nawly.Services.ServicesImplementation.Courier;
using Nawly.Services.ServicesImplementation.Delivery;
using Nawly.Services.ServicesImplementation.Dispatch.StrategyPattern;
using Nawly.Services.ServicesImplementation.Finance;
using Nawly.Services.ServicesImplementation.Geography;
using Nawly.Services.ServicesImplementation.Notification;
using Nawly.Services.ServicesImplementation.Notification.StrategyPattern;
using Nawly.Services.ServicesImplementation.Order;
using Nawly.Services.ServicesImplementation.Payment;
using Nawly.Services.ServicesImplementation.Pricing.StrategyPattern;
using Nawly.Services.ServicesImplementation.Profile;
using Nawly.Services.ServicesImplementation.Promotion;
using Nawly.Services.ServicesImplementation.Review;
using Nawly.Services.ServicesImplementation.Store;
using Nawly.Services.ServicesImplementation.Support;
using Nawly.Services.ServicesImplementation.Token;
using Nawly.Services.Validators;

namespace Nawly.Services;

/// <summary>
/// Registers Core application services, business policies/strategies,
/// FluentValidation validators, and in-process domain event handlers.
/// </summary>
public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<RequestOtpDtoValidator>();

        // Policies & Strategies
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IPricingPolicy, DefaultPricingPolicy>();
        services.AddSingleton<ICommissionPolicy, DefaultCommissionPolicy>();
        services.AddScoped<IDispatchStrategy, WaveDispatchStrategy>();
        services.AddScoped<INotificationStrategy, InAppNotificationStrategy>();
        services.AddScoped<INotificationStrategy, SmsNotificationStrategy>();
        services.AddScoped<INotificationStrategy, WebPushNotificationStrategy>();

        // Domain Event Dispatcher & Handlers
        services.AddScoped<IDomainEventDispatcher, InProcessDomainEventDispatcher>();
        services.AddScoped<IDomainEventHandler<OrderAcceptedEvent>, OrderAcceptedEventHandler>();
        services.AddScoped<IDomainEventHandler<OrderPreparingEvent>, OrderPreparingEventHandler>();
        services.AddScoped<IDomainEventHandler<OrderReadyForPickupEvent>, OrderReadyForPickupEventHandler>();
        services.AddScoped<IDomainEventHandler<OrderPickedUpEvent>, OrderPickedUpEventHandler>();
        services.AddScoped<IDomainEventHandler<OrderDeliveredEvent>, OrderDeliveredEventHandler>();
        services.AddScoped<IDomainEventHandler<OrderRejectedEvent>, OrderRejectedEventHandler>();
        services.AddScoped<IDomainEventHandler<OrderCancelledEvent>, OrderCancelledEventHandler>();
        services.AddScoped<IDomainEventHandler<DeliveryAssignedEvent>, DeliveryAssignedEventHandler>();
        services.AddScoped<IDomainEventHandler<DispatchEscalatedEvent>, DispatchEscalatedEventHandler>();

        // Core Application Services
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IGeographyService, GeographyService>();
        services.AddScoped<IAddressService, AddressService>();
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<IStoreService, StoreService>();
        services.AddScoped<ICatalogService, CatalogService>();
        services.AddScoped<ICartService, CartService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IDeliveryService, DeliveryService>();
        services.AddScoped<ICourierService, CourierService>();
        services.AddScoped<IPromotionService, PromotionService>();
        services.AddScoped<IFinanceService, FinanceService>();
        services.AddScoped<IReviewService, ReviewService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<ISupportService, SupportService>();
        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<IAttachmentService, AttachmentService>();

        return services;
    }
}
