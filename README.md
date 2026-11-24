# ShopPulse

**Smart retail operations for independent South African spaza, tuck and convenience shops.**

> Smarter operations. Less waste. Better business.

ShopPulse is a .NET 10 retail operations application built around the daily work of a small shop: managing products and stock, completing sales, recording refunds and waste, reconciling cash shifts, and reviewing business activity. Monetary values use South African Rand (ZAR).

**Project status:** the core staff-operated workflows are implemented in the API and Blazor client, and the main-branch CI pipeline is passing. ShopPulse is still a prototype/assessment project, not approved for processing live shop transactions. Production browser acceptance, operational recovery procedures and security review remain required. The public read-only guest experience is not implemented yet; the current home route is staff sign-in. Do not expose tenant inventory or operational data publicly.

## Technology

| Area | Implementation |
|---|---|
| Runtime | .NET 10 |
| Frontend | Blazor WebAssembly |
| Backend | ASP.NET Core Web API |
| Persistence | Entity Framework Core |
| Local development | SQLite with synthetic seed data |
| Production database | PostgreSQL (Npgsql) |
| Authentication | Short-lived JWT access token and HttpOnly refresh-token cookie |
| Authorization | Owner/Operator roles plus server-enforced operator permissions |
| Tests | xUnit, SQLite-based authorization tests and PostgreSQL migration/refresh integration test |
| Deployment packaging | Dockerfiles for API and client; GitHub Actions CI |

## Implemented application journeys

The following journeys have corresponding client pages and API workflows in the current repository:

| Area | Route | Implemented behaviour |
|---|---|---|
| Staff sign-in and overview | `/` | Sign-in, session restoration, API status and authenticated product overview |
| Product management | `/products/manage` | Create, edit and deactivate products; SKU and price validation |
| Checkout | `/sales` | Product search, basket, cash/card method selection, server-side stock validation, FEFO batch allocation, idempotent checkout and printable receipt |
| Refunds | `/sales/refunds` | Full refunds with reason and payment confirmation; optional restocking of original batches |
| Inventory | `/inventory` | Product quantities, stock batches and stock movement history |
| Receive stock | `/stock/receive` | Receive a batch with quantity, unit cost, supplier/reference and optional expiry |
| Record waste | `/stock/waste` | Batch-specific write-off with reason, stock movement and audit record |
| Expiry monitoring | `/expiry` | Expired, expiring-soon and safe stock batches |
| Stock Rescue | `/stock-rescue` | Rules-based replenishment/rescue suggestions based on recent sales and expiry; does not change stock or prices |
| Cash shifts | `/cash-shift` | Open a shift, record cash in/out, review movements and reconcile/close |
| Daily reports | `/reports` | Owner-only sales, refunds, payment mix, waste, inventory and cash-variance summary |
| Audit history | `/audit` | Owner-only, read-only business audit events |
| Staff management | `/staff` | Owner creates Operators, activates/deactivates accounts and assigns permissions |
| Business settings | `/settings` | Owner updates business name and address |

### Important scope boundaries

- **Guest access:** no separate anonymous, read-only public catalogue or guest journey exists yet. The root route is the staff sign-in experience. A future guest surface must remain read-only and use deliberately public data; operational APIs must remain authenticated.
- **Payments:** cash is recorded by ShopPulse. Card checkout records an operator's confirmation that an external terminal approved the payment; there is no acquiring-bank/payment-gateway integration and ShopPulse does not collect card credentials.
- **Refunds:** full refunds are implemented; partial refunds are not.
- **Reporting:** the current report is a daily summary. It does not provide historical inventory snapshots.
- **Idempotency:** checkout uses an idempotency key and supports retry after an uncertain response. Other write workflows do not yet share a uniform idempotency contract; a lost response on receiving, waste, cash movement or refund requires careful reconciliation.
- **Production readiness:** no live-browser end-to-end acceptance run, backup-restore exercise or commercial POS operational sign-off is claimed.

## Roles and security

The API is the security boundary. Client-side navigation and hidden controls are usability features only; direct API requests are authorized server-side.

| Capability | Owner | Operator |
|---|---:|---:|
| View products and inventory | Yes | Yes |
| Complete sales | Yes | Yes |
| Manage products | Yes | With `CanManageProducts` |
| Receive stock | Yes | With `CanReceiveStock` |
| Record waste | Yes | With `CanAdjustStock` |
| Refund sales | Yes | With `CanRefundSales` |
| Open/close own cash shift | Yes | Yes |
| Review/reconcile another operator's open shift | Yes | No |
| View financial reports and audit history | Yes | No |
| Manage staff and business settings | Yes | No |

Security controls in the current source include:

- Required production JWT signing key (at least 32 UTF-8 bytes) and explicit HTTPS CORS origins.
- BCrypt password hashing, login throttling/lockout and short-lived access tokens.
- HttpOnly refresh cookie, hashed refresh-token storage, rotation and reuse/replay handling.
- Active user and business status rechecked during authenticated requests.
- Tenant-scoped EF Core query filters and business-aware permission handlers.
- Audit events for sensitive business operations.
- Production startup fails closed when PostgreSQL configuration or EF Core migrations are missing.

These controls still require full security review and deployed-origin browser verification before real shop use.

## Repository structure

```
ShopPulse/
├── .github/workflows/dotnet.yml
├── docs/
│   ├── Complete_Design.html
│   ├── DEPLOYMENT_HANDOFF.md
│   ├── DEPLOY_LIVE_URL.md
│   ├── FRONTEND_BACKEND_INTEGRATION_AUDIT.md
│   ├── FRONTEND_UX_REVIEW.md
│   ├── PR1_IMPLEMENTATION_AND_VERIFICATION.md
│   ├── ShopPulse_Solution_Design_Samsung_2027.html
│   └── USER_JOURNEYS.md
├── ShopPulse.slnx
├── src/
│   ├── ShopPulse.Api/
│   │   ├── Auth/
│   │   ├── Authorization/
│   │   ├── Controllers/
│   │   ├── Data/
│   │   ├── Domain/
│   │   └── Migrations/
│   └── ShopPulse.Client/
│       ├── Auth/
│       ├── Pages/
│       ├── Shared/
│       └── wwwroot/
└── tests/
    └── ShopPulse.Api.Tests/
```

The current solution has two application projects (API and client) plus a test project. Domain entities and EF Core persistence are currently housed in the API project; separate Domain, Application and Infrastructure projects have not been created. The repository is therefore not yet a fully separated Clean Architecture solution.

## Local development

Requirements: .NET 10 SDK.

Restore, build and run the automated tests from the repository root:

```bash
dotnet restore ShopPulse.slnx
dotnet build ShopPulse.slnx --configuration Release --no-restore
dotnet test ShopPulse.slnx --configuration Release --no-build
```

The API launch profile uses port `5162`; the Blazor client launch profile uses port `5080`. For local development:

1. Run the API in Development. It uses SQLite, creates the local schema and seeds synthetic demo data.
2. Set `ApiBaseUrl` in `src/ShopPulse.Client/wwwroot/appsettings.json` to the local API origin (for example `http://localhost:5162/`) while developing locally.
3. Allow the client origin `http://localhost:5080` in the API's Development CORS configuration (for example, with `Cors__Origins__0`).
4. Run the Blazor client and open its local URL.

Restore the checked-in API base URL before committing if you temporarily change it for local testing. Never use synthetic development accounts or data for a real business.

## Database and migrations

- Development uses SQLite, `EnsureCreatedAsync()` and synthetic seed data.
- Non-Development environments require PostgreSQL configuration and at least one EF Core migration; startup applies migrations with `Database.MigrateAsync()` and does not insert demo seed data.
- The initial PostgreSQL migration and `AppDbContextModelSnapshot` are present under `src/ShopPulse.Api/Migrations/`.
- CI runs a PostgreSQL-backed migration integration test using a disposable schema and also exercises concurrent refresh-token rotation.
- The presence of a migration and a passing CI run do not replace a controlled production backup, migration review and restore plan.

## Configuration

Set production values in the hosting provider's private environment-variable manager. Do not commit secrets or paste them into chat, logs or issues.

| Environment variable | Purpose |
|---|---|
| `ASPNETCORE_ENVIRONMENT` | Set to `Production` for the hosted API |
| `UseSqlite` | Set to `false` outside Development |
| `ConnectionStrings__Default` | PostgreSQL/Npgsql connection string |
| `Jwt__SigningKey` | Random private signing secret of at least 32 UTF-8 bytes |
| `Cors__Origins__0` | Exact HTTPS origin of the deployed frontend; add indexed entries only for other trusted origins |
| `PORT` | Injected by the container host; the API binds to `0.0.0.0:$PORT` |

The client reads `ApiBaseUrl` from `wwwroot/appsettings.json`. The client Docker image also supports runtime substitution from an `ApiBaseUrl` environment variable using its entrypoint script.

## Build and publish

From the repository root:

```bash
dotnet restore ShopPulse.slnx
dotnet build ShopPulse.slnx --configuration Release --no-restore
dotnet test ShopPulse.slnx --configuration Release --no-build
dotnet publish src/ShopPulse.Api/ShopPulse.Api.csproj --configuration Release --no-restore --output artifacts/api
dotnet publish src/ShopPulse.Client/ShopPulse.Client.csproj --configuration Release --no-restore --output artifacts/client
```

For a static Blazor host, the published site files are in `artifacts/client/wwwroot`. The client also has an nginx Dockerfile at `src/ShopPulse.Client/Dockerfile`.

Container build commands (run from the repository root):

```bash
docker build -t shoppulse-api:local src/ShopPulse.Api
docker build -t shoppulse-client:local src/ShopPulse.Client
```

## CI verification

GitHub Actions runs on pushes to `main` and `fix/**`, and pull requests targeting `main`. The workflow restores, builds, tests, publishes both projects and builds both Docker images. Its PostgreSQL service is PostgreSQL 17.

The test project currently contains ten authorization/permission tests and one PostgreSQL migration/refresh-token integration test. The PostgreSQL test is skipped when `SHOPPULSE_TEST_POSTGRES_CONNECTION` is not supplied; CI supplies it. This is useful backend coverage, but there is no browser-driven end-to-end test suite in the repository yet.

**Latest verified main CI:** commit `ee01d8fd5bd144fbe2c7fde455b01cc4b0928c41` completed successfully on 24 November 2026, including restore, build, tests, API/client publish and both container builds. [View the run](https://github.com/Thando21610/ShopPulse/actions/runs/45413767191). This verifies that commit's CI pipeline only; it does not verify a live deployment.

## Deployment direction

The repository includes a .NET 10 API Dockerfile and a Blazor client Dockerfile. The documented hosting direction is:

- **API:** Railway container service, with Root Directory/build context `src/ShopPulse.Api`.
- **Database:** Neon PostgreSQL, configured privately through Railway variables.
- **Frontend:** publish Blazor WebAssembly static assets and deploy the contents of `artifacts/client/wwwroot` to the configured static host, or deploy the client container where appropriate.

Before deploying the API, verify the Railway root directory, PostgreSQL connection, `UseSqlite=false`, JWT signing secret and exact frontend CORS origin. After deployment, verify both `/health` (liveness) and `/ready` (database connectivity). Then test sign-in, session restoration after browser reload, refresh, logout, permissions and tenant isolation from the real frontend origin.

Do not deploy against production data or onboard a real shop until migrations, backups/restore, monitoring, cross-origin cookie behaviour and all operational workflows have passed acceptance testing. Railway's free tier should be treated as a prototype/demo environment, not a guaranteed always-on POS host.

## Product principles

- Keep public/guest experiences read-only; keep all retail operations authenticated.
- Treat the customer as a participant in the shop's sales process, not as a separate portal user unless requirements explicitly change.
- Enforce permissions in the API, not only in the UI.
- Scope every business-owned operation to the authenticated business.
- Use server-side validation, database transactions and concurrency controls for critical stock/cash operations.
- Preserve an audit trail for sensitive actions.
- Use synthetic data until a pilot owner approves a controlled real-world pilot.
- Display all prices, sales, refunds, cash movements and reports in South African Rand (R/ZAR).
- Do not call a feature production-complete until its UI, API, authorization, tests and deployed behaviour have been verified.

## Project documentation

- [Solution design](docs/ShopPulse_Solution_Design_Samsung_2027.html)
- [User journeys and use-case coverage](docs/USER_JOURNEYS.md)
- [Frontend/API integration audit](docs/FRONTEND_BACKEND_INTEGRATION_AUDIT.md)
- [Frontend UX review](docs/FRONTEND_UX_REVIEW.md)
- [Deployment handoff](docs/DEPLOYMENT_HANDOFF.md)
- [Railway + Neon deployment notes](docs/DEPLOY_LIVE_URL.md)
- [PR #1 implementation and verification record](docs/PR1_IMPLEMENTATION_AND_VERIFICATION.md)

## Links

- [GitHub repository](https://github.com/Thando21610/ShopPulse)
- [GitHub Actions](https://github.com/Thando21610/ShopPulse/actions)
- [Production frontend](https://shoppulse-bpd.pages.dev/)
- [Production API](https://shoppulse-production-e6673892.up.railway.app/health)
