# Fieldore — Backend (.NET 8, Clean Architecture)

API for the Fieldore field-service app. The mobile frontend is a separate Expo repo at `d:\Developer\Fieldore` that consumes this API via an orval-generated client built from this project's Swagger. Keep Swagger accurate — frontend types are generated from it.

## Projects
- **Fieldore.API** — ASP.NET Core controllers, `Program.cs` (JWT, CORS `AllowAll`, Swagger, static files), hosts on `http://0.0.0.0:5166`.
- **Fieldore.Application** — feature contracts: `I*Service` interfaces + request/response `record` DTOs (`<Feature>/Contracts/*`). Shared `ApiResponse<T>` / `PagedResponse<T>` live in `Auth/Contracts/ApiResponse.cs`.
- **Fieldore.Domain** — entities (inherit `AuditableEntity`: Guid `Id`, `CreatedAt`, `UpdatedAt`), value objects (`Address`), and string constants (`*Statuses`, `*Roles`).
- **Fieldore.Infrastructure** — EF Core `FieldoreDbContext`, service implementations, `Auth/` (PBKDF2 `PasswordHasher`, `TokenService`), `Migrations/`, DI in `Extensions/ServiceCollectionExtensions.cs`.

## Conventions (mirror the Invoice vertical for any new feature)
- Controllers are thin: `[Authorize]`, `[Route("api/[controller]")]`, `TryGetUserId(out var userId)` from `ClaimTypes.NameIdentifier`, then delegate to the service and return `ApiResponse<T>`.
- Services take `FieldoreDbContext` via primary constructor and **always scope by business**: `GetBusinessIdAsync(userId)` (`Businesses.Where(x => x.AuthUserId == userId)`), null → 404.
- Validation is **manual**: private static `Validate…` methods returning `string?` (null = valid, otherwise a message returned as a 400). No FluentValidation.
- Money is `decimal(18,2)`; IDs are Guid (`ValueGeneratedOnAdd`); timestamps auto-applied in `SaveChanges`. Multi-tenancy is enforced in app code (no global query filters yet).
- Numbers (invoice/estimate/job) are auto-generated per business via `Generate…NumberAsync`.
- File uploads: see `JobsController.AddPhoto` — save under `/uploads/<feature>/<id>/`, store the relative path, return an absolute URL.
- Register every new service in `ServiceCollectionExtensions.AddInfrastructure` (`services.AddScoped<IFooService, FooService>()`).
- Public client pages (accept quote, pay invoice) are served by THIS API as `[AllowAnonymous]` token-based endpoints/pages — not the mobile app.

## Data model notes
- Entities already present but **not yet exposed via services/controllers**: `Estimate`+`EstimateLineItem`, `PaymentRecord` (invoice payments, supports partial), `Lead`, `BusinessMembership` (workers: roles owner/admin/manager/technician/staff), `ServiceCatalogItem`.
- Workers are lightweight: `AppUserProfile` + `BusinessMembership` with **no `AuthUser`/login**.
- No `Expense` entity yet — to be added.
- Invoice statuses are being standardized to `draft, sent, viewed, partially_paid, paid, overdue, void` (was `draft/sent/unpaid/paid/overdue/void`).

## Commands
- `dotnet build`; run from `Fieldore.API`.
- Migrations: `dotnet ef migrations add <Name> -p Fieldore.Infrastructure -s Fieldore.API` then `dotnet ef database update -p Fieldore.Infrastructure -s Fieldore.API`.
- Provider: SQL Server (connection string in `Fieldore.API/appsettings.json`).

## Known must-fixes
- `TokenService` sets JWT `expires: DateTime.MaxValue` and `JwtSettings.SecretKey` is a hardcoded default — add real expiry/refresh and move secret to secrets/env.
- Stripe keys (to be added) must live in user-secrets/env, and the webhook must verify signatures.

## Roadmap & memory
Active roadmap: `C:\Users\sande\.claude\plans\please-scan-my-app-abundant-elephant.md`. Project facts/decisions in Claude memory (`MEMORY.md` index).
