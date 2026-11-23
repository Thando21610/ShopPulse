# ShopPulse deployment and production-readiness handoff

**Repository:** [Thando21610/ShopPulse](https://github.com/Thando21610/ShopPulse)  
**Branch inspected:** `main` (23 November 2026)  
**Purpose:** Continue deployment work in a new ChatGPT conversation without losing the verified state, blockers, configuration decisions or test plan.

> **Status: NOT YET APPROVED FOR PRODUCTION.** This is a deployment preparation handoff. Do not onboard a real shop, store real customer/staff data, or process live sales until the migration, security, deployment and end-to-end gates in this document have passed.

---

## 1. Start here in the next conversation

Repository: https://github.com/Thando21610/ShopPulse  
Deployment guide: [docs/DEPLOY_LIVE_URL.md](DEPLOY_LIVE_URL.md)  
Implementation record: [docs/PR1_IMPLEMENTATION_AND_VERIFICATION.md](PR1_IMPLEMENTATION_AND_VERIFICATION.md)  
GitHub Actions: https://github.com/Thando21610/ShopPulse/actions

Tell the next assistant:

1. Read this handoff and the current `main` branch before changing code.
2. Re-check the current repository state; do not assume the recorded state has not changed.
3. Work in small, reviewable commits/PRs. Do not commit credentials or production data.
4. First resolve the EF Core migration blocker and prove the API builds/tests against PostgreSQL.
5. Deploy and verify the API before publishing the Blazor frontend.
6. Wire the real frontend origin into API CORS and test authentication, cookies and session behaviour in a browser.
7. Report exact commands, commits, CI results, deployment URLs and test outcomes. Distinguish passed, failed, blocked and not run.

## 2. Verified repository snapshot

The inspected solution is .NET 10 and contains:

- `src/ShopPulse.Api/ShopPulse.Api.csproj` — ASP.NET Core API.
- `src/ShopPulse.Client/ShopPulse.Client.csproj` — Blazor WebAssembly client.
- `tests/ShopPulse.Api.Tests/ShopPulse.Api.Tests.csproj` — xUnit API test project.
- `ShopPulse.slnx` — includes the API, client and test project.
- `.github/workflows/dotnet.yml` — restore, Release build, test and publish steps.
- `src/ShopPulse.Api/Dockerfile` — .NET 10 multi-stage API image.
- `docs/DEPLOY_LIVE_URL.md` — initial Railway + Neon deployment notes.

### Current verified product scope

The README says the currently implemented user-facing surface is authentication and a live product catalogue. The API exposes login, refresh/logout, authenticated-user and product-related functionality, plus `/health`. The client has a sign-in and product-catalogue experience.

Do not describe these planned workflows as complete without inspecting and testing their API, UI, permissions and business logic:

- Checkout and transactional stock deduction.
- Receiving and stock adjustments.
- Waste recording.
- Cash-shift opening, reconciliation and closing.
- Financial/operational reporting.
- Staff administration and complete endpoint-level RBAC.

The app is currently a two-project application plus tests; separate Domain, Application and Infrastructure projects are not present in the inspected solution. Do not claim Clean Architecture layering has already been completed.

### Important documentation inconsistencies to correct

The inspected `README.md` contains stale statements that conflict with the current repository:

- It says EF Core migrations replacing `EnsureCreatedAsync()` are still needed, and later says the API currently calls `EnsureCreatedAsync()`. Current `Program.cs` uses `EnsureCreatedAsync()` only in Development; non-Development startup requires at least one migration and calls `MigrateAsync()`.
- It says no dedicated automated test project was identified. The current solution includes `tests/ShopPulse.Api.Tests/ShopPulse.Api.Tests.csproj`. Inspect its actual test files and coverage before describing test quality.
- The test project references SQLite EF Core. This is not proof of PostgreSQL integration coverage.

Update README and the older implementation record only after verifying the actual tests and migration files. Preserve the distinction between a test project existing and meaningful coverage passing.

## 3. Critical deployment blocker: EF Core migration

The current production startup code does this:

- Development: calls `EnsureCreatedAsync()` and inserts synthetic demo data.
- Non-Development: reads `db.Database.GetMigrations()`; if there are no migrations, startup throws an explicit error; otherwise it calls `Database.MigrateAsync()`.
- Demo seed data is not run in non-Development.

The repository state inspected for this handoff did not establish that an initial migration exists. A previous check of the expected initial migration path returned not found. **Re-check the actual `src/ShopPulse.Api/Migrations/` directory and model snapshot on the current branch.** Do not deploy until a reviewed migration and snapshot are committed.

### Migration work sequence

1. Inspect `AppDbContext`, all entity configurations, relationships, indexes, decimal precision, enum/string mappings, delete behaviours and tenant-related data requirements.
2. Confirm the API project references EF Core Design and the PostgreSQL provider (both were present in the inspected project file).
3. Confirm the .NET 10 SDK and EF CLI are available in the development environment. Install/use a compatible `dotnet-ef` tool if required.
4. Generate an initial migration from the current model using the API project as both the target and startup project. Typical command, run from the repository root:

   ```bash
   dotnet ef migrations add InitialPostgres \
     --project src/ShopPulse.Api/ShopPulse.Api.csproj \
     --startup-project src/ShopPulse.Api/ShopPulse.Api.csproj \
     --output-dir Migrations
   ```

   If design-time creation fails, diagnose the actual exception and add an explicit design-time DbContext factory only if needed. Do not invent a migration by hand to bypass model discovery.

5. Review the generated migration and `AppDbContextModelSnapshot.cs`. Confirm all current entities, primary/foreign keys, indexes, required fields, string lengths, decimal precision and PostgreSQL-compatible column types.
6. Check for SQLite-specific assumptions, unsupported column types, default values, date/time conversions and provider-specific SQL.
7. Apply the migration to a disposable PostgreSQL database first, not the only Neon production database.
8. Verify schema creation and run tests against PostgreSQL. SQLite tests are useful but do not substitute for provider-specific integration tests.
9. Commit migration and snapshot. Confirm CI builds and tests the migration commit.
10. Only then plan the first Neon schema deployment. Back up/export any existing Neon data first. Do not casually reset or drop the Neon database.

Do not run database migrations in the Docker build. Image builds must not mutate a live database. For a small single-instance prototype, the current startup migration mechanism may run migrations when the API starts; for a more controlled production deployment, use a deliberate one-off migration job and prevent multiple replicas from racing to migrate.

## 4. Hosting architecture and deployment order

Target prototype architecture:

1. GitHub repository and GitHub Actions for source control/CI.
2. Railway service for the ASP.NET Core API, built from the API Dockerfile.
3. Neon PostgreSQL for persistent relational storage.
4. Separate static hosting for the published Blazor WebAssembly client.
5. Browser calls from the frontend to the Railway API over HTTPS.

Railway Free is a prototype/demo option with usage and availability limits; it should not be represented as dependable always-on hosting for a commercial POS. Check the current Railway and Neon plan limits before deployment. Do not store critical live shop operations on a sleeping/free-tier setup without an appropriate reliability and recovery plan.

### Phase A — pre-deployment repository gate

- [ ] Confirm intended deployment branch is `main`.
- [ ] Pull latest main and inspect uncommitted changes.
- [ ] Confirm .NET 10 SDK.
- [ ] Run restore, Release build, tests and publish for both projects.
- [ ] Inspect GitHub Actions result for the exact commit being deployed.
- [ ] Resolve README/deployment-document contradictions.
- [ ] Confirm initial PostgreSQL migration and snapshot are present, reviewed and tested.
- [ ] Confirm no credentials, .env files, local database files, real user data or build artifacts are tracked.
- [ ] Confirm Dockerfile build context and runtime configuration.

Suggested local commands:

```bash
dotnet --info
dotnet restore ShopPulse.slnx
dotnet build ShopPulse.slnx --configuration Release --no-restore
dotnet test ShopPulse.slnx --configuration Release --no-build
dotnet publish src/ShopPulse.Api/ShopPulse.Api.csproj -c Release
dotnet publish src/ShopPulse.Client/ShopPulse.Client.csproj -c Release
```

Run the commands separately and retain their actual output. A successful publish is not proof that the app connects to Neon or works in a browser.

### Phase B — Neon database preparation

- [ ] Open the correct ShopPulse Neon project and production branch.
- [ ] Verify database name, role, region and compute status in the Neon dashboard.
- [ ] Ensure the previously exposed database password has been rotated. If uncertain, rotate it again.
- [ ] Use the pooled hostname for normal application connections.
- [ ] Store the new connection secret only in Railway Variables (and a secure password manager if desired).
- [ ] Use a PostgreSQL disposable/test database for migration rehearsal.
- [ ] Apply the reviewed migration and verify tables/indexes.
- [ ] Establish a backup/export and restore test before real data is introduced.

Never paste a password, complete connection string, `.env` file or secret into chat, GitHub, source files or screenshots. If a secret is exposed, rotate it; deleting a message does not make the credential safe again.

### Phase C — Railway API service

The API Dockerfile is at `src/ShopPulse.Api/Dockerfile`. Set the Railway service Root Directory to exactly:

```
src/ShopPulse.Api
```

Confirm Railway uses the repository's Dockerfile. The Dockerfile uses the .NET 10 SDK/runtime and the API reads Railway's injected `PORT`, binding to `0.0.0.0:$PORT`.

Set these variables in Railway's private Variables panel:

| Variable | Required value / guidance |
|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `UseSqlite` | `false` |
| `ConnectionStrings__Default` | Npgsql key/value connection string built from the current Neon pooled host, database, role and rotated password |
| `Jwt__SigningKey` | Fresh random secret, at least 32 UTF-8 bytes; never reuse a value exposed in chat |
| `Cors__Origins__0` | Exact deployed frontend origin, e.g. scheme + hostname only; do not use the Railway API URL unless the frontend is actually hosted there |

Expected Npgsql connection-string shape (placeholders only):

```
Host=YOUR_NEON_POOLER_HOST;Database=YOUR_DATABASE;Username=YOUR_ROLE;Password=YOUR_ROTATED_PASSWORD;SSL Mode=Require;Channel Binding=Require
```

The Neon dashboard may show a `postgresql://` URI. The API uses Npgsql's key/value connection-string format through `UseNpgsql(conn)`; use a provider-compatible Npgsql format. Do not assume the URI can be pasted unchanged.

Do not use the previously shared JWT key; it is now considered exposed. Generate a new secret using a cryptographically secure random generator and store it only in Railway. Keep the issuer/audience aligned with the API configuration unless intentionally changed in both token creation and validation.

#### CORS: important exact value

The checked-in API appsettings currently lists local development origins:

- `http://localhost:5000`
- `https://localhost:5001`

Those are not the production frontend address. For Railway, `Cors__Origins__0` must be the actual static frontend's origin, for example `https://your-site.example` (example only). Do not guess it before the static host has assigned the domain. Do not include a path or trailing slash. If there are multiple trusted frontend origins, configure indexed entries such as `Cors__Origins__1`. Do not use `*`, because the API allows credentials.

The production startup code throws if the configured CORS origin list is empty. The checked-in local defaults may still be loaded by configuration, but leaving them in place will not allow a separately hosted production frontend to call the API.

#### Railway network and health

- Generate a Railway public domain for the API.
- Configure the health check path as `/health`.
- Deploy only after the migration gate is passed.
- Verify `https://YOUR-API-DOMAIN/health` returns HTTP 200.

Current `/health` is a liveness endpoint only. It returns a simple service status and does not check database connectivity, migration state, or downstream dependencies. Do not interpret HTTP 200 as proof that PostgreSQL is ready.

### Phase D — Blazor WebAssembly frontend

The client is a static WebAssembly app and should be deployed separately from the API.

The inspected file `src/ShopPulse.Client/wwwroot/appsettings.json` currently contains a placeholder:

```json
{
  "ApiBaseUrl": "https://YOUR-API-URL.up.railway.app"
}
```

Before publishing:

1. Replace the placeholder with the actual HTTPS Railway API base URL, following the exact URL convention used by the client's API service registrations (check whether it expects a trailing slash).
2. Verify every API client/service uses this configured base URL and no localhost or placeholder URL is hard-coded elsewhere.
3. Publish the client in Release mode.
4. Deploy the contents of the Blazor publish output's `wwwroot` directory to a static host.
5. Configure the static host's fallback/rewrite so Blazor client-side routes resolve to `index.html`.
6. Enable HTTPS and record the exact assigned frontend origin.
7. Set Railway `Cors__Origins__0` to that exact frontend origin, redeploy/restart the API and verify browser preflight and API requests.
8. Verify that no secrets are embedded in the client. Blazor WebAssembly configuration is public to every visitor.

The final frontend host is not yet selected in this handoff. Choose a static host that supports SPA fallback and HTTPS; do not invent a live frontend URL.

## 5. Authentication, cookies and browser acceptance tests

The API uses JWT bearer authentication and an HttpOnly refresh-token cookie design. The inspected README and implementation record state that the Blazor client currently keeps the access token in memory; a browser reload loses that token and automatic refresh/session restoration is incomplete.

This is a known functional gap, not merely a deployment variable issue. Before calling the deployment production-ready, inspect and implement the complete client refresh flow. Test with the real frontend and API origins, not only localhost.

Cross-origin cookie requirements to verify include:

- Cookie `SameSite` and `Secure` settings appropriate for the actual hosting origins.
- Client requests that need the refresh cookie send credentials (`credentials: include` / equivalent in the actual .NET client implementation).
- API CORS uses the exact origin and `AllowCredentials()` (currently configured).
- Cookie path and expiry match the refresh/logout endpoints.
- Refresh-token rotation, replay handling, expiry and logout invalidation work.
- No access token or refresh token is logged or exposed to JavaScript unnecessarily.

Test these browser flows:

1. Sign in with a valid test account.
2. Confirm authenticated API requests succeed.
3. Reload the browser and confirm the expected session restoration behaviour.
4. Wait for/force access-token expiry and verify refresh works.
5. Sign out and verify refresh cookie/session is invalidated.
6. Attempt an authenticated request after logout and confirm it is rejected.
7. Test invalid credentials, expired refresh token, revoked token, network interruption and API downtime.
8. Inspect browser DevTools Network and Application/Cookies tabs for CORS, cookie and HTTPS issues. Do not share screenshots containing tokens or secrets.

Use synthetic test users only. Production does not receive the Development seed accounts.

## 6. Test strategy and release gates

The solution includes an xUnit test project, but its project file references SQLite EF Core. Inspect the test source files to identify what is actually covered. Do not equate test-project presence with complete coverage.

### Required test layers

**A. Build/CI**

- Restore solution.
- Release build.
- Run all tests.
- Publish API and client.
- Verify CI is green for the exact commit intended for deployment.

**B. API and authorization tests**

- Login success and invalid credentials.
- Refresh, token rotation, logout and revoked refresh token.
- 401 for unauthenticated access.
- Role and permission checks for every protected endpoint.
- Owner/operator differences.
- Validation and error response consistency.
- Rate limiting for authentication endpoints.

**C. Tenant isolation**

Create two separate synthetic businesses and users. Prove that a user from Business A cannot read, update, delete or infer Business B's data by changing IDs, request bodies, query parameters or related entity IDs. Cover users, products, stock batches, movements, sales, waste, cash shifts, audit events and idempotency records. Review entities without global query filters and every use of `IgnoreQueryFilters()`. Verify writes and relationships as well as reads.

**D. PostgreSQL integration tests**

Run against a disposable PostgreSQL database using the same provider family/version as Neon. Test:

- Migration from an empty database.
- API startup after migration.
- EF Core mappings and queries.
- Decimal precision for ZAR amounts.
- Date/time and DateOnly handling.
- Unique constraints and indexes.
- Transactions and concurrency.
- Refresh-token persistence.
- Tenant filters and RBAC.
- Rollback/recovery behaviour for failed critical operations.

SQLite in-memory tests do not prove PostgreSQL compatibility.

**E. Frontend and end-to-end tests**

- API base URL points to the deployed API.
- HTTPS and CORS preflight succeed.
- Sign-in, catalogue retrieval, loading/error/empty states.
- Refresh/reload/logout flows.
- Role-sensitive UI is consistent with API enforcement.
- No broken navigation or console errors.
- Mobile viewport layout works.
- Currency displays as South African Rand (ZAR).
- No placeholder data, fake metrics or development credentials in the deployed experience.

### Current feature-scope rule

Only mark a business workflow complete when its backend implementation, database transaction behaviour, validation, RBAC, tenant isolation, audit trail, frontend interaction and automated tests all work together. In particular, do not enable checkout or real stock/cash operations simply because the catalogue page is deployed.

## 7. Security and operational readiness

Before any real pilot data:

- Rotate exposed Neon credentials and use fresh secrets.
- Use a new high-entropy JWT signing key.
- Restrict CORS to exact trusted origins.
- Enforce HTTPS for both frontend and API.
- Verify authorization and tenant isolation server-side.
- Review refresh-token storage, hashing, rotation and revocation.
- Avoid logging passwords, bearer tokens, cookies or connection strings.
- Configure database backups and perform a restore rehearsal.
- Establish monitoring for API errors, database failures and resource limits.
- Decide how schema migrations are run and rolled back.
- Confirm data retention, privacy notices, consent and POPIA responsibilities.
- Use least-privilege database credentials where supported; avoid using an owner/admin role for routine application traffic if a restricted role can be provisioned safely.
- Document incident response and credential rotation.
- Review Railway/Neon plan limits and expected cost before relying on the service.

## 8. Known limitations and open decisions

| Area | State at handoff | Next action |
|---|---|---|
| EF Core migration | Not verified as present; production startup blocks if absent | Inspect migrations directory; generate/review/test initial migration |
| PostgreSQL integration | Not verified | Add/run tests against disposable PostgreSQL |
| Railway API | Not verified deployed | Configure service after migration gate |
| Neon connection | User has configured variables, but connectivity not independently verified | Verify from deployed API without exposing secrets |
| Frontend host | Not selected/verified | Choose static host and deploy published `wwwroot` |
| API base URL | Client config contains placeholder | Replace with actual Railway API URL before publish |
| CORS origin | Local defaults in repo; production origin unknown | Set exact static frontend origin |
| Refresh/session restoration | Known incomplete in client | Implement and browser-test before production |
| Health endpoint | Liveness only | Consider separate database readiness check |
| Test coverage | xUnit project exists; project references SQLite | Inspect test cases and add PostgreSQL/RBAC/tenant tests |
| Retail workflows | Several planned flows incomplete | Implement and verify each end-to-end before enabling |
| Production backups/restore | Not verified | Configure and test restore |
| Railway Free suitability | Prototype/demo only | Upgrade/choose production-grade hosting before live commercial use |

## 9. Final deployment acceptance checklist

Do not mark deployment complete until every relevant item is evidenced:

- [ ] Latest intended `main` commit identified.
- [ ] README and deployment docs reconciled with code.
- [ ] Initial EF Core migration and model snapshot committed.
- [ ] Migration successfully applied to disposable PostgreSQL.
- [ ] PostgreSQL integration tests pass.
- [ ] GitHub Actions passes for the exact release commit.
- [ ] Railway Root Directory is `src/ShopPulse.Api`.
- [ ] Railway variables are configured privately with fresh secrets.
- [ ] API uses PostgreSQL, not SQLite.
- [ ] API deploy succeeds and logs show no startup/migration errors.
- [ ] API `/health` returns HTTP 200.
- [ ] Frontend published in Release and deployed to static host.
- [ ] Client `ApiBaseUrl` points to the correct API.
- [ ] Exact frontend origin configured in API CORS.
- [ ] Browser preflight and API calls succeed over HTTPS.
- [ ] Login, refresh, reload, logout and expiry flows pass.
- [ ] RBAC and cross-business isolation tests pass.
- [ ] No secrets or demo credentials are exposed.
- [ ] Backup and restore procedure is tested.
- [ ] Known incomplete retail workflows remain disabled/not advertised as complete.

## 10. Useful commands

Run from repository root unless noted:

```bash
# Inspect SDK
dotnet --info

# Restore, build and test
dotnet restore ShopPulse.slnx
dotnet build ShopPulse.slnx -c Release --no-restore
dotnet test ShopPulse.slnx -c Release --no-build

# Publish API
dotnet publish src/ShopPulse.Api/ShopPulse.Api.csproj -c Release

# Publish Blazor WebAssembly client
dotnet publish src/ShopPulse.Client/ShopPulse.Client.csproj -c Release

# Inspect migration state
dotnet ef migrations list \
  --project src/ShopPulse.Api/ShopPulse.Api.csproj \
  --startup-project src/ShopPulse.Api/ShopPulse.Api.csproj
```

For any failing command, capture the first meaningful error and the full relevant output. Fix the underlying issue rather than suppressing warnings or skipping tests.

---

**Handoff rule:** The next conversation should begin by re-reading the current repository and this document, then work from the first unchecked release gate. Do not assume deployment, migration, CI, database connectivity or browser acceptance has succeeded until the result is directly observed.


---

## 9. Follow-up verification — 23 November 2026

The current `main` branch was rechecked after the original handoff was written.

- `src/ShopPulse.Api/Migrations/AppDbContextModelSnapshot.cs` exists.
- The expected initial migration file `src/ShopPulse.Api/Migrations/20261123000000_InitialPostgres.cs` does **not** exist (GitHub returned 404 for that exact path). Re-list the migrations directory before generating anything, to ensure no differently named migration has since been added.
- The fetched model snapshot contains SQLite-style store types such as `TEXT` and `INTEGER`, while the intended production provider is Npgsql/PostgreSQL. Treat this as a provider-specific snapshot requiring careful regeneration/verification under the Npgsql provider; do not assume it is a valid PostgreSQL migration snapshot.
- `src/ShopPulse.Client/wwwroot/appsettings.json` still contains the placeholder API URL `https://YOUR-API-URL.up.railway.app`; it must be replaced with the actual API origin before publishing the client.
- Root `START_HERE.md` previously contained dangerous instructions to recursively delete repository-root contents and copy an unverified ZIP, plus a Railway `UseSqlite=true` instruction. That file has now been replaced with safe instructions that prohibit destructive repository replacement and require Neon PostgreSQL for the hosted deployment. See [START_HERE.md](../START_HERE.md).

These checks do not establish a successful Railway deployment, successful PostgreSQL connection, or successful Release build. Those outcomes still require direct build, CI and database verification.
