# PR #1 — implementation and verification record

**Repository:** [Thando21610/ShopPulse](https://github.com/Thando21610/ShopPulse)  
**Merged PR:** [#1 — Harden deployment configuration and add .NET CI gate](https://github.com/Thando21610/ShopPulse/pull/1)  
**Target branch:** `main`  
**Merge commit:** `6607bf264a6b3bd4569601fbfed6b64c9c3f287b`  
**Record prepared:** 23 November 2026

This document records the changes included in PR #1, what they do, the verification mechanism added, and what remains unverified or incomplete. It describes the repository state inspected on `main`; it is not a claim that the application has passed a production acceptance test.

---

## 1. Executive summary

PR #1 focused on a pre-deployment quality and safety baseline:

1. Require a real JWT signing secret in non-development environments instead of silently using a known fallback.
2. Apply EF Core tenant filters consistently so model caching cannot omit tenant restrictions.
3. Replace the starter-style landing/dashboard with a responsive sign-in and product-catalogue workspace backed by the API.
4. Remove unused Blazor starter/weather pages and improve not-found, validation, and toast feedback.
5. Add a GitHub Actions workflow to restore, build, test, and publish both .NET projects.
6. Update the repository README to describe the application and its current implementation boundaries.

These changes improve the deployment baseline. They do **not** make every planned ShopPulse retail workflow complete, and they do not by themselves establish that production deployment is safe.

## 2. Changes delivered

### 2.1 JWT signing-key hardening

**Files:** `src/ShopPulse.Api/Program.cs`, `src/ShopPulse.Api/Auth/AuthService.cs`

The API now reads the signing key from `Jwt:SigningKey`.

- In non-development environments, startup fails if the setting is absent.
- The configured key must contain at least 32 UTF-8 bytes.
- The development-only fallback key is explicitly labelled local-only.
- `AuthService` also requires the configured signing key instead of silently substituting a hard-coded value.

**Why:** a known fallback secret could allow an attacker who knows the source code to forge tokens if the application were deployed without overriding it.

**Deployment requirement:** configure `Jwt__SigningKey` as a secret in the hosting platform. Use a randomly generated secret with at least 32 bytes. Do not commit the production value to Git.

### 2.2 Tenant query-filter consistency

**File:** `src/ShopPulse.Api/Data/AppDbContext.cs`

The DbContext exposes the current business identifier through a context property and always adds tenant filters to the model for these tenant-owned entities:

- `AppUser`
- `Product`
- `StockBatch`
- `StockMovement`
- `Sale`
- `WasteRecord`
- `CashShift`
- `AuditEvent`
- `IdempotencyRecord`

When there is no authenticated business context, the current business identifier resolves to `Guid.Empty`; ordinary tenant records therefore do not match a real business identifier.

**Why:** EF Core caches its model. Building a model conditionally based on the first scoped request can result in filters being absent from a cached model used by later requests. Keeping the filter definitions in the model avoids that particular failure mode.

**Important limitations to address before production:**

- `Business` and `RefreshToken` do not have global query filters in the inspected DbContext. Access to these records must continue to be carefully scoped and tested.
- Global query filters are not a substitute for checking ownership of identifiers supplied by a client, validating relationships, or reviewing every query that uses `IgnoreQueryFilters()`.
- The current implementation needs automated cross-business isolation tests, including tests for related entities and refresh-token operations.
- Tenant isolation must be reviewed across writes as well as reads.

### 2.3 Responsive sign-in and product workspace

**Files:** `src/ShopPulse.Client/Pages/Home.razor`, `src/ShopPulse.Client/wwwroot/css/app.css`

The root page now provides:

- ShopPulse branding and a retail-workspace layout.
- API health status and a service-unavailable state.
- A sign-in form with email/password validation.
- Sign-in feedback for invalid credentials, service errors, and unexpected responses.
- An authenticated workspace showing the signed-in user's display name and role.
- A product catalogue loaded from `GET api/products`.
- Loading, error, empty-catalogue, and retry states.
- Product name, SKU, available quantity, and unit price columns.
- South African Rand formatting for prices.
- Sign-out UI and success/error toast notifications.
- Responsive styling and accessible status/notification semantics.

The previous unsupported dashboard figures were removed rather than displaying invented sales or stock metrics.

**API calls currently used by the page:**

| Purpose | Request |
|---|---|
| API availability | `GET health` |
| Sign in | `POST api/auth/login` |
| Load catalogue | `GET api/products` |
| Sign out | `POST api/auth/logout` |

The client stores the access token in the in-memory `HttpClient.DefaultRequestHeaders.Authorization` header. This means a browser reload loses the in-memory bearer token. Although the API has refresh-token endpoints and an HttpOnly cookie design, the client does not yet implement a complete automatic refresh/session-restoration flow. This must be completed and tested before treating the session experience as production-ready.

### 2.4 API access policy currently visible in the code

**Files:** `src/ShopPulse.Api/Program.cs`, `src/ShopPulse.Api/Controllers/ProductsController.cs`

The API registers these authorization policies:

- `OwnerOnly`: requires the `Owner` role.
- `Staff`: allows `Owner` or `Operator`.

The products controller is protected by `[Authorize(Policy = "Staff")]`. The client displays the role returned by the API, but the display itself is not a security boundary; authorization must be enforced by the API.

The presence of these policies does not mean every planned operation has complete role-based restrictions. Review and test all future stock adjustment, waste, cash-shift, reporting, and user-administration endpoints when they are implemented.

### 2.5 Removed unused starter pages and improved feedback

The PR commit history records removal of unused Blazor starter/weather pages and a not-found experience polish. It also records accessibility improvements to validation and success/error toast notifications.

The intention is to keep the visible application focused on the ShopPulse retail workflow rather than leaving unrelated template screens in the navigation or user journey.

### 2.6 Continuous integration workflow

**File:** `.github/workflows/dotnet.yml`

The workflow is named **.NET build and test** and is configured for:

- Pushes to `main` and `fix/**`.
- Pull requests targeting `main`.

It runs on `ubuntu-latest` and installs the .NET 10 SDK. Its steps are:

1. Check out the repository.
2. Install .NET 10.
3. Restore `ShopPulse.slnx`.
4. Build the solution in Release configuration without restoring again.
5. Run `dotnet test` in Release configuration without rebuilding.
6. Publish the API project to `artifacts/api`.
7. Publish the Blazor client project to `artifacts/client`.

This provides an automated build/publish gate. The workflow file alone is not proof that a run completed successfully.

**Test-coverage limitation:** the inspected solution currently contains the API and client projects, but no dedicated automated test project was identified. Therefore, a successful `dotnet test` invocation must not be described as comprehensive unit/integration test coverage. Add test projects and meaningful assertions for authentication, tenant isolation, authorization, and retail calculations.

## 3. Current application structure and responsibilities

The repository is currently a two-project .NET solution:

| Project | Responsibility |
|---|---|
| `src/ShopPulse.Api` | ASP.NET Core Web API, authentication, EF Core data access, domain entity definitions, database provider configuration, seeding, health endpoint |
| `src/ShopPulse.Client` | Blazor WebAssembly UI and API HTTP client configuration |

The API project currently contains the domain entity classes and EF Core DbContext. Controllers access the DbContext directly. The repository is therefore **not yet a fully separated Clean Architecture solution** with distinct Domain, Application, and Infrastructure projects. That is a future architectural improvement, not something delivered by PR #1.

### 3.1 Domain model represented in the API

The entity definitions include:

- Business and AppUser
- Product, StockBatch, and StockMovement
- Sale and SaleItem
- WasteRecord
- CashShift
- RefreshToken
- IdempotencyRecord
- AuditEvent

The model includes monetary decimal fields configured for two-decimal precision, tenant/business identifiers, stock-batch quantities, expiry dates, and audit metadata. The presence of entities in the model does not mean all related workflows or API endpoints are implemented.

### 3.2 Database configuration

The API supports SQLite and PostgreSQL provider configuration. SQLite is the default local/demo option. PostgreSQL is selected by setting `UseSqlite=false` and providing `ConnectionStrings__Default`.

The current startup code calls `EnsureCreatedAsync()` and then seeds synthetic data. This is convenient for an early demo but is not a substitute for a controlled EF Core migration strategy in a production system. A persistent production database and tested migrations are required before real shop data is stored.

### 3.3 Synthetic seed data

The API seeds a synthetic pilot business, an Owner account, an Operator account, and eight example products with stock batches when the database has no businesses.

The repository README documents demo credentials. These are public demo credentials and must never be reused for real accounts or production access. Disable or replace demo seeding for a real deployment, and use unique credentials.

## 4. API behaviour inspected on main

The following routes were present in the inspected source:

| Method | Route | Access / purpose |
|---|---|---|
| GET | `/health` | Health response |
| POST | `/api/auth/login` | Anonymous, rate-limited login |
| POST | `/api/auth/refresh` | Anonymous refresh-cookie exchange |
| POST | `/api/auth/logout` | Revokes refresh token when supplied and clears cookie |
| GET | `/api/auth/me` | Authenticated user profile |
| GET | `/api/products` | Owner/Operator policy; active product catalogue, optional `q` filter |

The login endpoint returns an access token, expiry, and user object, and writes a refresh token to an HttpOnly cookie. Refresh and logout use the configured cookie options. The products endpoint calculates available stock as the sum of remaining quantities across the product's batches.

The client and API should be exercised together in an integration environment. Source inspection is not a substitute for an end-to-end HTTP test.

## 5. Configuration and deployment requirements

### Required API configuration

| Setting | Requirement |
|---|---|
| `Jwt__SigningKey` | Secret value of at least 32 UTF-8 bytes |
| `Cors__Origins__0` (or equivalent `Cors:Origins` array configuration) | Exact deployed frontend origin |
| `UseSqlite` | `true` for local/demo SQLite, `false` for PostgreSQL |
| `ConnectionStrings__Default` | PostgreSQL connection string when SQLite is disabled |

The API intentionally fails startup outside Development if the JWT signing key is missing or if no production CORS origins are configured.

### Hosting notes

- API deployment instructions currently describe Railway or Render using the API Dockerfile.
- Static Blazor WebAssembly hosting is described for Vercel or Cloudflare Pages.
- Set the client `ApiBaseUrl` to the actual API origin, then republish/redeploy the static client.
- Configure the exact frontend origin in API CORS settings.
- Use a persistent managed PostgreSQL database for production. A container's local SQLite file can be lost when the instance is replaced unless persistent storage is deliberately configured.
- Verify HTTPS, cookie behaviour, CORS, refresh-token rotation, and browser privacy settings on the real deployed domains.

See [DEPLOY_LIVE_URL.md](DEPLOY_LIVE_URL.md) for the existing deployment walkthrough. Review it against the current source before following it: hosting instructions are operational guidance, not evidence that deployment has occurred.

## 6. Verification status and how to verify

### What has been confirmed from repository inspection

- PR #1 is merged into `main`.
- The merge commit is `6607bf264a6b3bd4569601fbfed6b64c9c3f287b`.
- The CI workflow file exists on `main`.
- The workflow targets .NET 10 and contains restore, build, test, API publish, and client publish steps.
- The API and client project files target `net10.0`.

### What has not been confirmed by this record

- A successful GitHub Actions run for the merge commit.
- A clean Release build in a local .NET 10 environment.
- A passing automated test suite.
- API/client end-to-end login and product-list behaviour on deployed origins.
- Production database migration and persistence.
- Full role-permission coverage or cross-tenant security testing.
- A production deployment.

Check the [GitHub Actions page](https://github.com/Thando21610/ShopPulse/actions) for the run associated with the merge commit. The desired result is a completed green workflow with all build and publish steps successful. If the workflow is missing, queued, or failed, do not mark the build as passed.

### Local/Codespaces verification commands

From the repository root:

```bash
dotnet --info
dotnet restore ShopPulse.slnx
dotnet build ShopPulse.slnx --configuration Release --no-restore
dotnet test ShopPulse.slnx --configuration Release --no-build
dotnet publish src/ShopPulse.Api/ShopPulse.Api.csproj --configuration Release --no-restore --output artifacts/api
dotnet publish src/ShopPulse.Client/ShopPulse.Client.csproj --configuration Release --no-restore --output artifacts/client
```

Then run the API and client, configure the client's `ApiBaseUrl`, and verify health, login, refresh, logout, and product listing using both Owner and Operator test identities. Also test invalid credentials, expired access tokens, missing refresh cookies, forbidden roles, and attempts to access another business's records.

## 7. Known gaps and recommended next work

The following are not represented as completed by PR #1:

1. **Automated tests:** add unit and integration test projects. Prioritise tenant isolation, JWT validation, refresh-token rotation/reuse detection, role policies, and product stock aggregation.
2. **Tenant security review:** add explicit filters or tightly scoped access for refresh tokens and review business lookup paths, navigation queries, writes, and any query-filter bypasses.
3. **RBAC coverage:** define a permission matrix and enforce every permission server-side, especially stock adjustments, user administration, waste, cash reconciliation, and reports.
4. **Client session lifecycle:** implement silent refresh, token renewal coordination, safe handling of expired sessions, and proper logout behaviour. Verify cross-site HttpOnly cookie settings with the deployed frontend/API domains.
5. **Database migrations:** replace production reliance on `EnsureCreatedAsync()` with versioned migrations and a controlled deployment process.
6. **Production persistence:** configure managed PostgreSQL, backups, connection security, and restore testing.
7. **Architecture separation:** progressively introduce Domain, Application, and Infrastructure boundaries and keep HTTP controllers thin. Avoid a large untested rewrite.
8. **Retail workflows:** complete and test checkout, FEFO stock deduction, stock receiving, waste recording, cash-shift close, and reporting before describing them as operational.
9. **Observability:** add structured logs, correlation IDs across requests, health/readiness checks, and safe error monitoring.
10. **Operational security:** remove or disable demo credentials in production, rotate secrets, validate cookie/CORS/HTTPS settings, and establish data retention and backup policies.

## 8. Change history represented by PR #1

The merged PR history includes work in these areas:

- Production JWT secret configuration and validation.
- Consistent EF Core tenant query filters.
- Responsive ShopPulse workspace and removal of placeholder dashboard metrics.
- Removal of unused Blazor starter/weather pages.
- Not-found page polish.
- Accessible form validation and success/error toast notifications.
- GitHub Actions .NET build/test/publish workflow.
- README and deployment/verification documentation updates.

This record groups related commits by outcome rather than duplicating every individual commit message.

---

## 9. Definition of done for the next release

Do not call ShopPulse production-ready until all of the following are true:

- [ ] GitHub Actions passes on the current `main` SHA.
- [ ] Dedicated automated tests run and pass.
- [ ] Cross-tenant isolation tests pass for every tenant-owned entity and endpoint.
- [ ] Role/permission tests prove both allowed and denied operations.
- [ ] Client login, refresh, expiry, and logout work on deployed HTTPS domains.
- [ ] PostgreSQL migrations, backups, and restore procedure are verified.
- [ ] The implemented retail workflows have end-to-end tests.
- [ ] Production secrets and demo credentials are handled safely.
- [ ] Deployment health and rollback procedures are documented.

**Status:** PR #1 is merged. The CI definition and code changes are present on `main`; successful main-branch build status and production readiness remain separate verification gates.
