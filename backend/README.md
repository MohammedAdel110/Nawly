# Nawly Backend

ASP.NET Core Web API (.NET 10) for the **Nawa Delivery Platform** — see `../Nawa_Delivery_Platform_SRS.pdf`.
Clean Architecture following the PharmaBridge reference structure.

```
backend/
├─ Nawly.slnx · global.json · Directory.Build.props
├─ Core/
│  ├─ Nawly.Domain/        Entities (Models/<module>), ValueObjects, Events, Exceptions,
│  │                       Contracts (GenericRepos, SpecificRepos, Specification, UnitOfWork, DbInitializer)
│  ├─ Nawly.Abstraction/   IServices/<feature>  – service interfaces + ports (Sms, Storage, Payment, Dispatch, Pricing)
│  └─ Nawly.Services/      ServicesImplementation/<feature>, Specifications, Mapping, Validators, EventHandlers
├─ Infrastructure/
│  ├─ Nawly.Persistence/   NawlyDbContext, Configurations/<module>, Migrations, Interceptors,
│  │                       Implementations (Repos, UoW, Initializer), Evaluator, Seeds, ProgramExtensions
│  └─ Nawly.Presentation/  Controllers/<area>, Filters, Extensions
├─ Nawly.Shared/           DTOs/<feature>, Common (Pagination, Params, Response, Results), EnumHelper
└─ Nawly.Web/              Program.cs (composition root), Extensions, Hubs (SignalR), Middleware
```

**Dependency direction:** Shared ← Domain / Abstraction ← Services ← Persistence; Abstraction/Domain ← Presentation; Persistence + Presentation ← Web.

**Domain modules (SRS §6):** Identity, Geography, Catalog, Ordering, Delivery, Payments, Promotions, Finance, Engagement, Platform.

## Phase 1 — Core Layer Implementation

Implemented across seven modular feature branches merged with `--no-ff` into `main`:

1. `feat/core-foundation-and-contracts` — Shared enums, `Result`/`Error`, `ApiResponse`, pagination wrappers, domain base entities (`BaseGuidEntity`, `AggregateRoot` with UUID v7), value objects (`Money`, `GeoPoint`, `PhoneNumber`, `ArabicNormalizer`), specification & repository/UoW contracts, domain exceptions.
2. `feat/core-domain-identity-geography-catalog` — `AppUser`/`AppRole`, `CustomerProfile`, `CourierProfile`, `RefreshToken`, `OtpChallenge`, `DeliveryZone`/`Area`/`Address`, `Store`, `MenuCategory`, `Product`, option groups/options, opening hours (Cairo time).
3. `feat/core-domain-ordering-delivery` — `Cart`/`CartItem`, `Order`/`OrderItem`/`OrderStatusHistory` guarded state machine, `Delivery`/`DeliveryOffer`/`CourierShift`/`CourierLocationHistory`, domain events, specific repository contracts (`IOrderRepository`, `IDeliveryRepository`, `IStoreRepository`).
4. `feat/core-domain-payments-finance-engagement` — `Payment`/`PaymentWebhookEvent`/`Refund`, `Coupon`/`CouponRedemption`, immutable `LedgerEntry`/`Settlement`/`SettlementLine` & `ILedgerRepository`, `Review`/`Favorite`/`Notification`/`WebPushSubscription`/`SupportTicket`/`SupportMessage`/`Banner`, `AuditLog`/`OutboxMessage`/`IdempotencyRecord`/`SystemSetting`.
5. `feat/core-shared-dtos-and-abstractions` — Query parameter filters (`BaseQueryParam` subclasses), feature DTOs in `Nawly.Shared`, and all 29 service/strategy/port interfaces in `Nawly.Abstraction`.
6. `feat/core-services-specifications-mapping-validators` — Reusable `BaseSpecifications` query objects, static DTO mapping extensions (no AutoMapper), and FluentValidation validators.
7. `feat/core-services-implementations` — Pricing/commission/wave-dispatch/notification strategies, in-process domain event dispatcher & handlers, Core application service implementations, `AddApplicationServices()` DI registration wired in `Nawly.Web/Program.cs`.

> **Note:** Infrastructure-backed contracts (`IAuthService`, `ITokenService`, `IPaymentService`, `IClock`, `ICurrentUser`, `ISmsSender`, `IFileStorage`, `IPaymentGateway`, `IRealtimeNotifier`) and repositories (`IUnitOfWork`, `IOrderRepository`, etc.) are ready for Phase 2 (`Infrastructure/Nawly.Persistence` and `Nawly.Web`).

## Run

```powershell
dotnet build Nawly.slnx
dotnet run --project Nawly.Web     # Scalar UI at /scalar/v1 (Development)
```

Migrations (once the DbContext exists):

```powershell
dotnet ef migrations add <Name> -p Infrastructure/Nawly.Persistence -s Nawly.Web -o Migrations
```
