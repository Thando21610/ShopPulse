# ShopPulse

**Smart retail operations for independent South African spaza, tuck and convenience shops.**

> Smarter operations. Less waste. Better business.

ShopPulse is an evolving retail operations platform. Its currently implemented application surface is intentionally limited to authentication and a live product catalogue. Sales, receiving, waste, cash reconciliation and reporting must not be treated as complete until their API workflows, permissions, tests and UI are implemented.

## Project profile

| Area | Current direction |
|---|---|
| Currency | South African Rand (ZAR) |
| Frontend | Blazor WebAssembly (.NET 10) |
| Backend | ASP.NET Core Web API (.NET 10) |
| Data access | Entity Framework Core |
| Local/demo database | SQLite |
| Production database target | PostgreSQL with durable managed storage |
| Authentication | JWT access token + HttpOnly refresh-token cookie (client session restoration still incomplete) |
| Architecture | Currently a two-project application solution; Clean Architecture separation is planned, not yet completed |

## Repository structure

```
ShopPulse/
├── .github/workflows/dotnet.yml
├── docs/
│   ├── PR1_IMPLEMENTATION_AND_VERIFICATION.md
│   └── DEPLOY_LIVE_URL.md
├── ShopPulse.slnx
├── src/
│   ├── ShopPulse.Api/
│   └── ShopPulse.Client/
└── tests/
    └── ShopPulse.Api.Tests/
```

Domain entities and EF Core persistence currently live inside the API project; separate Domain, Application and Infrastructure projects have not yet been introduced.

## Current implemented scope

- Login, refresh, logout and authenticated-user endpoints exist in the API.
- The API exposes a health endpoint.
- Authenticated Owner and Operator staff can retrieve active products through the Staff policy.
- The Blazor home page provides sign-in and a responsive product catalogue.
- Product prices are displayed in South African Rand.
- Tenant query filters are defined for tenant-owned entities.
- Production startup requires a configured JWT signing key and explicit CORS origins.
- A dedicated API test project includes authorization and cross-business isolation tests.

The frontend currently keeps the access token in memory. A browser refresh loses that token; automatic refresh and session restoration in the Blazor client still need to be implemented and tested.

## Not yet production-complete

The following are planned work, not claims of implemented functionality:

- Checkout with transactional stock deduction, FEFO batch allocation and idempotency.
- Stock receiving and adjustments.
- Waste recording.
- Cash-shift opening, reconciliation and closing.
- Financial and operational reports.
- Staff administration and complete endpoint-level permission enforcement.
- Separate Domain, Application and Infrastructure layers.
- An initial EF Core migration for PostgreSQL. Production startup now requires migrations and runs `Database.MigrateAsync()`; no initial migration is currently present.
- PostgreSQL integration tests, including migration verification against a disposable PostgreSQL database.
- Durable PostgreSQL deployment, backup/restore verification and operational monitoring.
- Complete refresh-cookie/session behaviour across the deployed frontend and API origins.

See [the PR #1 implementation and verification record](docs/PR1_IMPLEMENTATION_AND_VERIFICATION.md) and the [Railway + Neon deployment guide](docs/DEPLOY_LIVE_URL.md) for inspected routes, current security controls and deployment prerequisites.

## Local development

Use the .NET 10 SDK.

### API

```bash
cd src/ShopPulse.Api
dotnet restore
dotnet run
```

The local/demo configuration defaults to SQLite. In Development, the API uses `EnsureCreatedAsync()` and synthetic seed data. The API health route is `/health`.

### Blazor client

Set `ApiBaseUrl` in `src/ShopPulse.Client/wwwroot/appsettings.json` to the local API origin, then run:

```bash
cd src/ShopPulse.Client
dotnet restore
dotnet run
```

Synthetic seed accounts are for local demonstration only. Never reuse their credentials for a real shop or production deployment. Do not put real customer or staff personal information into demo data.

## Configuration and deployment safety

### Required production API settings

| Setting | Purpose |
|---|---|
| `Jwt__SigningKey` | Private random signing secret of at least 32 UTF-8 bytes |
| `Cors__Origins__0` (and further indexed entries if needed) | Exact deployed frontend origin, including scheme and host |
| `UseSqlite` | Set to `false` for PostgreSQL |
| `ConnectionStrings__Default` | PostgreSQL connection string supplied by the hosting platform |

Keep secrets in the hosting provider's secret/environment-variable manager. Never commit them or send them in chat.

**Do not deploy the current SQLite configuration to an ephemeral container for real shop data.** Use PostgreSQL with durable managed storage, backups and a tested restore procedure. Do not enable wildcard CORS origins when credentials/cookies are used.

In Development, the API uses EF Core `EnsureCreatedAsync()` for the local SQLite demo. In Production, startup requires at least one EF Core migration and applies migrations with `Database.MigrateAsync()`; it deliberately fails closed if no migration is present. The initial migration has not yet been generated and tested, so production deployment is still blocked.

### Hosting direction

The current deployment target is a containerised .NET 10 API on Railway Free with Neon PostgreSQL, followed by a separately hosted static Blazor WebAssembly frontend. Railway Free is suitable for a prototype/demo rather than an always-on commercial POS. See [Railway + Neon deployment guide](docs/DEPLOY_LIVE_URL.md).

For the Railway API service, set **Root Directory** to exactly `src/ShopPulse.Api`. The Dockerfile is in that directory and expects the API project folder to be the Docker build context. This is a Railway service setting; a GitHub merge does not set it automatically. Configure the exact frontend origin in API CORS and validate refresh-cookie behaviour across the deployed origins before launch.

## Build and test status

The GitHub Actions workflow is configured to restore, build, test and publish the API and client using .NET 10. A workflow definition is not evidence that a run passed.

- The API test project contains authorization tests, including role/permission and cross-business isolation coverage.
- PostgreSQL-specific migration/integration testing is still required; SQLite tests do not prove PostgreSQL compatibility.
- The latest Railway deployment attempt failed at build configuration because the service root directory was not set to `src/ShopPulse.Api`. This is a Railway configuration failure, not a verified GitHub Actions result.
- No production deployment or end-to-end acceptance test is being claimed.

Check [GitHub Actions](https://github.com/Thando21610/ShopPulse/actions) and the Railway service deployment logs for the latest results.

## Product and security principles

- Enforce every permission in the API; hiding a button in the UI is not authorization.
- Scope every business-owned read and write to the authenticated business.
- Use server-side validation, transactions, concurrency controls and idempotency for critical stock and payment operations.
- Preserve an audit trail for sensitive business actions.
- Use synthetic data until a pilot owner has explicitly approved a real-world pilot.
- Display all monetary values in ZAR.
- Do not claim a workflow is complete until its API, UI, authorization and tests work end to end.

## Links

- [GitHub repository](https://github.com/Thando21610/ShopPulse)
- [GitHub Actions](https://github.com/Thando21610/ShopPulse/actions)
- [Implementation and verification record](docs/PR1_IMPLEMENTATION_AND_VERIFICATION.md)
- [Deployment guide](docs/DEPLOY_LIVE_URL.md)
