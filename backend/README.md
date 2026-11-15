# Nawly Backend

ASP.NET Core Web API (.NET 10) for the **Nawa Delivery Platform** — see `../Nawa_Delivery_Platform_SRS.pdf`.
Clean Architecture, same layout as the PharmaBridge reference project.

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

## Run

```powershell
dotnet build Nawly.slnx
dotnet run --project Nawly.Web     # Scalar UI at /scalar/v1 (Development)
```

Migrations (once the DbContext exists):

```powershell
dotnet ef migrations add <Name> -p Infrastructure/Nawly.Persistence -s Nawly.Web -o Migrations
```
