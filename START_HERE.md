# ShopPulse — Start Here (Current Deployment Instructions)

**Status: deployment preparation only. Do not deploy this repository to production until the PostgreSQL migration and release gates pass.**

This file supersedes any older phone/Codespaces instructions that tell you to delete the repository contents, upload a replacement ZIP, or deploy with SQLite.

## Important safety warning

Do **not** run commands such as `find . ... -exec rm -rf` against the repository root. They can delete tracked source code, documentation, configuration and GitHub workflow files.

Do not upload a ZIP over the existing repository structure or replace the current `src/` and `docs/` folders with an unverified archive.

Do not configure Railway with `UseSqlite=true`. Railway container storage is not the durable production database. ShopPulse's intended hosted database is Neon PostgreSQL.

## Current repository layout

- `src/ShopPulse.Api` — ASP.NET Core .NET 10 API and Dockerfile.
- `src/ShopPulse.Client` — Blazor WebAssembly frontend.
- `tests/ShopPulse.Api.Tests` — xUnit tests.
- `docs/DEPLOYMENT_HANDOFF.md` — detailed continuation plan and verified limitations.
- `docs/DEPLOY_LIVE_URL.md` — Railway + Neon deployment guide.
- `ShopPulse.slnx` — solution.

## Current deployment blocker

Production startup in `src/ShopPulse.Api/Program.cs` requires at least one EF Core migration and calls `Database.MigrateAsync()`. The current `main` branch has an EF Core model snapshot but no initial migration file at the expected path. The snapshot currently contains SQLite-style column types such as `TEXT` and `INTEGER`; it must be checked/regenerated for the PostgreSQL provider as part of creating the actual PostgreSQL migration.

**Do not deploy the API to Production or point it at the Neon database until a PostgreSQL migration has been generated, reviewed and applied successfully to a disposable PostgreSQL database.**

## Correct order of work

1. Inspect the latest `main` branch and working tree.
2. Review the EF Core model, current snapshot and provider configuration.
3. Generate a PostgreSQL migration using the Npgsql provider; review the migration and snapshot.
4. Run Release restore/build/tests/publish and add PostgreSQL integration verification.
5. Apply the migration to a disposable PostgreSQL database and verify schema and API startup.
6. Reconcile README and deployment documentation with actual code and test results.
7. Configure Railway API service with Root Directory `src/ShopPulse.Api`, `ASPNETCORE_ENVIRONMENT=Production`, `UseSqlite=false`, a private Npgsql `ConnectionStrings__Default`, and a fresh private `Jwt__SigningKey`.
8. Deploy and verify API health and database connectivity.
9. Publish the Blazor client, deploy its `wwwroot` output to static hosting, and configure SPA fallback.
10. Set `Cors__Origins__0` to the exact HTTPS frontend origin and verify login, refresh-cookie, logout, RBAC and tenant-isolation flows in a real browser.

## Railway configuration notes

- API Root Directory: `src/ShopPulse.Api`
- Database: Neon PostgreSQL (not SQLite)
- `UseSqlite=false`
- `ConnectionStrings__Default`: Npgsql key/value connection string stored only in Railway Variables.
- `Jwt__SigningKey`: fresh cryptographically random secret of at least 32 UTF-8 bytes, stored only in Railway Variables. Never reuse a key exposed in chat.
- `Cors__Origins__0`: exact deployed frontend origin, including `https://`, without a path or trailing slash. Do not guess this before the frontend host assigns its domain.
- Health path: `/health`. This is currently a liveness endpoint; HTTP 200 alone does not prove PostgreSQL readiness.

Never paste secrets, full connection strings or `.env` files into chat or commit them to GitHub. Rotate any credential that has been exposed.

## Next action

Start with the migration and PostgreSQL verification described in [DEPLOYMENT_HANDOFF.md](docs/DEPLOYMENT_HANDOFF.md). Record actual command output and distinguish passed, failed, blocked and not-run checks. Do not claim production readiness until all required gates have passed.
