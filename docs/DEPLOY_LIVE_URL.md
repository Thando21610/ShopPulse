# ShopPulse deployment: Railway API + Neon PostgreSQL

> **Deployment status: preparation only.** Do not deploy production data until an initial EF Core migration has been created, reviewed and tested. The API now deliberately refuses to start in Production when no migrations exist. This prevents an empty production database from being silently created using EnsureCreated.

## Target architecture

- GitHub: source repository and CI.
- Railway Free: container host for the ASP.NET Core .NET 10 API.
- Neon: managed PostgreSQL database (the existing ShopPulse project, production branch, Singapore region).
- Blazor WebAssembly client: separate static hosting service after the API is verified.

Railway's free tier is usage-limited, not an always-on production tier. It currently provides $1 of monthly resource credit, services can sleep after 5–10 minutes of inactivity, and the service wakes on a subsequent request. A new account may first receive a 30-day trial with a one-time $5 grant. Check current limits before relying on it. See [Railway Free Plan](https://docs.railway.com/pricing/free-plan).

## Security: rotate the exposed Neon password

A Neon connection string containing a password was pasted into a chat. Treat that credential as compromised:

1. Open the Neon project.
2. Open the connection details for the production branch.
3. Reset the database role password.
4. Copy the new pooled connection string.
5. Never commit it to Git, put it in appsettings, or paste it into an issue or pull request.

Use the pooled hostname for normal API traffic. Keep the new URI in Railway's private Variables panel only.

## Repository and Docker configuration

The API Dockerfile is located at src/ShopPulse.Api/Dockerfile. In Railway, set the service Root Directory to src/ShopPulse.Api so the Dockerfile and project file are in the Docker build context.

The Docker image intentionally does not force SQLite. The API's checked-in appsettings still defaults to SQLite for local development, so Railway must explicitly set UseSqlite=false.

## Railway setup (mobile-friendly)

1. Sign in at [railway.com](https://railway.com) using GitHub.
2. Create a project and choose **Deploy from GitHub repo**.
3. Select Thando21610/ShopPulse and the deployment branch/commit you intend to deploy. Do not deploy this preparation branch to production before the migration work is completed.
4. Open the API service **Settings → Build** and set **Root Directory** to src/ShopPulse.Api.
5. Confirm Railway detects and builds Dockerfile.
6. Open **Variables** and add the values below. Use Railway's private variable editor; do not put secret values in GitHub.

| Variable | Value |
|---|---|
| ASPNETCORE_ENVIRONMENT | Production |
| UseSqlite | false |
| ConnectionStrings__Default | New Neon pooled connection string, rotated after exposure |
| Jwt__SigningKey | New random secret, at least 32 UTF-8 bytes |
| Cors__Origins__0 | Exact deployed Blazor client origin, including https:// |

Do not set Cors__Origins__0 to *. If the frontend has more than one trusted origin, add indexed entries such as Cors__Origins__1.

7. In **Settings → Networking**, generate a Railway domain.
8. In **Settings → Deploy**, set the healthcheck path to /health.
9. Deploy only after the EF migration gate described below has been satisfied.
10. Once active, open https://YOUR-RAILWAY-DOMAIN/health. The current endpoint is a liveness check; it does not yet prove database readiness.

Railway's ASP.NET Core guidance requires a Dockerfile for .NET and a listener bound to the injected PORT. ShopPulse's Program.cs reads PORT and binds to 0.0.0.0; the Dockerfile exposes the local fallback port 8080.

## PostgreSQL migration gate — required before first production deployment

The existing application previously used EnsureCreatedAsync(), which is not a production schema migration strategy. The API startup has now been changed so that:

- Development continues to use SQLite EnsureCreatedAsync() and synthetic seed data.
- Production does not run the demo seed.
- Production requires at least one EF Core migration and runs Database.MigrateAsync().
- Until a migration exists, Production startup intentionally fails with an explicit error.

Before deploying:

1. Add an initial migration for the current AppDbContext and verify it includes all current entities, indexes, relationships and query-filter-related schema requirements.
2. Review the generated migration and snapshot; do not generate it blindly against production.
3. Apply it to a disposable/test PostgreSQL database first.
4. Run the API integration tests against PostgreSQL, including tenant isolation and RBAC.
5. Confirm the migration succeeds against the Neon database before starting the API.
6. Decide how future migrations will run. For a single API instance, a controlled one-off migration command is preferable to having every replica race to migrate during startup.

Do not put dotnet ef database update into the Docker build. Build images should be repeatable and must not mutate production databases.

## Free-tier expectations

Railway Free is suitable for a prototype/demo with intermittent traffic, not a reliable always-on commercial POS. Its $1 monthly usage credit can be exhausted by CPU, memory and egress; after the credit is exhausted services stop until the next billing cycle. Idle services sleep and the first request after sleep can be slower. Neon Free also has its own compute and storage limits. Monitor both dashboards and keep backups/exports of any pilot data.

For a live shop handling real sales, cash and inventory, move to an appropriate paid hosting tier and verify backup/restore, monitoring, security and support requirements before onboarding real businesses.

## Blazor client — later step

After the API is deployed and verified:

1. Set the client's ApiBaseUrl to the Railway API URL in the client deployment configuration.
2. Publish the Blazor WebAssembly client as static files.
3. Deploy those files to a static host.
4. Set the API's Cors__Origins__0 to the exact static frontend origin.
5. Test login, refresh-cookie behaviour, logout, API calls and browser refresh from the real deployed origins. The refresh cookie is cross-origin-sensitive and must be verified in a real browser.

## Verification checklist

- [ ] Exposed Neon password rotated.
- [ ] Initial EF Core migration generated and reviewed.
- [ ] Migration applied successfully to a test PostgreSQL database.
- [ ] Production seed data is disabled.
- [ ] UseSqlite=false configured in Railway.
- [ ] New pooled Neon connection string stored only in Railway Variables.
- [ ] JWT signing key stored only in Railway Variables.
- [ ] Exact frontend CORS origin configured.
- [ ] Docker build succeeds.
- [ ] GitHub Actions restore/build/test/publish passes.
- [ ] PostgreSQL integration tests pass.
- [ ] /health returns HTTP 200.
- [ ] Login, refresh, logout and tenant boundaries verified end-to-end.
- [ ] Backups and restore procedure tested before real business data is used.

## Official references

- [Railway Free Plan](https://docs.railway.com/pricing/free-plan)
- [Railway ASP.NET Core deployment guide](https://railway.com/guides/aspnet-core)
- [Railway Dockerfiles](https://docs.railway.com/builds/dockerfiles)
- [Railway healthchecks](https://docs.railway.com/deployments/healthchecks)
- [Railway service configuration](https://docs.railway.com/services)
