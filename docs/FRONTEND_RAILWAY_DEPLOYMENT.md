# ShopPulse frontend deployment (Railway)

## What is included

The Blazor WebAssembly client is published as static files and served by Nginx. The Dockerfile and Nginx configuration live in this directory so Railway can build the frontend with this service root:

`src/ShopPulse.Client`

The client reads `ApiBaseUrl` from `wwwroot/appsettings.json`. The frontend container also supports a Railway runtime variable named `ApiBaseUrl`: its Nginx entrypoint writes that value into the published appsettings file at container startup. Set it on the frontend service so the API origin can be changed without rebuilding the image. Do not include an API route suffix.

## Railway frontend service

1. Create a new service from the `Thando21610/ShopPulse` GitHub repository.
2. Set the service Root Directory to `/src/ShopPulse.Client`.
3. Set Builder to Dockerfile if Railway does not detect it automatically.
4. Add `ApiBaseUrl=https://shoppulse-production-e6673892.up.railway.app` to the frontend service variables.
5. Deploy and wait for the image build and deployment to complete.
6. Generate a public domain in the service Networking settings.
7. Open the generated HTTPS domain and confirm the ShopPulse sign-in screen loads.

The container listens on port 80. Do not hardcode a Railway-assigned public port in the Nginx configuration.

## API CORS

After the frontend domain has been generated, set this variable on the API service:

- `Cors__Origins__0=https://YOUR-ACTUAL-FRONTEND-DOMAIN`

Use the exact origin only: scheme plus hostname, with no path and no trailing slash. Do not use `*`; the API allows credentials and must keep an explicit origin allow-list. Redeploy the API after changing the variable.

## Smoke tests

1. Open `https://YOUR-API-DOMAIN/health`; expect HTTP 200 and a small JSON health response.
2. Open `https://YOUR-API-DOMAIN/openapi/v1.json`; expect the OpenAPI document.
3. Open the frontend HTTPS domain; expect the sign-in page.
4. In browser developer tools, confirm the frontend calls the API origin rather than itself and that CORS does not block the request.
5. Submit a valid test login and confirm the product catalogue loads.
6. Verify invalid credentials show a useful error and that an unauthorised request cannot retrieve products.

## Implemented journeys and verification boundary

The repository now contains UI flows wired to the API for sign-in/catalogue, delegated product management, inventory/batch/movement views, checkout with Rush Mode, cash-shift enforcement, external card-terminal confirmation and receipt printing, stock receipt, batch waste recording, expiry prioritisation and Stock Rescue recommendations, full sale refunds with optional return-to-batch restocking, cash-shift opening/closing and cash refund reconciliation, owner financial summaries with refund totals, staff permission management, audit history and business settings. The API persists these operations, applies tenant query filters, records stock movements/audit events, and enforces server-side policies. Card payments and refunds are recorded only after the operator confirms approval on a separate card terminal; no bank/card gateway is integrated. Owners can delegate product management, stock receiving, waste recording and refunds to operators independently. The frontend includes a single-flight refresh handler, HttpOnly refresh-cookie restoration, shared success/error toasts, mobile layouts, and category-colour product initials rather than hero/product stock imagery. The visual brief intentionally excludes hero and remote product photos; no stock imagery dependency is added.

Partial refunds and anonymous queue management are not implemented. The current refund journey is a full-sale refund and requires the operator to confirm that money was returned externally through the original payment channel. Queue management remains optional in the solution design and should only be built if validated with the shop owner. These are implemented code paths, not proof of successful production acceptance. Verify each journey against the deployed Railway API and Neon database using separate Owner and Operator accounts. In particular, confirm the refresh cookie is accepted by the browser across the deployed origins, validate that operators cannot access owner-only actions, and test concurrent checkout/stock updates before using the system for live trading.

A successful static frontend deployment does not by itself prove Neon connectivity or database readiness. Confirm API deployment logs and test the authenticated catalogue flow against the deployed API.


## Database migration checkpoint

The production API runs reviewed EF Core migrations during startup. The current migration chain includes:

- `20261123102908_InitialPostgres`
- `20261123121000_AddSaleRefundSupport`

The second migration adds the delegated operator permission columns (`CanRefundSales`, `CanManageProducts`, `CanReceiveStock`) and the sale refund/concurrency fields. After the latest API deployment, verify both migration IDs are present in Neon:

```sql
SELECT "MigrationId"
FROM "__EFMigrationsHistory"
ORDER BY "MigrationId";
```

Do not reset or drop the Neon schema to apply this migration. If the API fails during startup migration, inspect the Railway deployment logs and Neon migration history before changing production data.

## End-to-end acceptance checklist

Run these checks with a dedicated Owner account and separate Operator accounts configured with different delegated permissions. Do not use synthetic Development seed accounts against production.

| Journey | Acceptance checks |
|---|---|
| Sign-in/session | Login, reload the browser, navigate to a protected route, wait for access-token expiry/401, verify one refresh per concurrent burst, then logout and confirm refresh is rejected |
| Catalogue/products | Owner and CanManageProducts operator can create/edit/deactivate; operator without permission receives 403; verify SKU uniqueness and ZAR prices |
| Receiving | Owner and CanReceiveStock operator create a batch; verify quantity, unit cost, supplier, reference, expiry and positive movement in Neon |
| Inventory | Verify product totals, batch details, expired stock exclusion from sellable quantity and movement ledger |
| Checkout | Cash sale requires open shift; card sale requires external terminal confirmation; verify idempotent retry creates one sale and stock is allocated FEFO |
| Price/stock conflict | Change a product price or sell the remaining batch concurrently; verify the operator must review updated price/availability before retrying |
| Waste | Operator with CanAdjustStock records a batch-specific write-off; operator without permission receives 403; verify negative movement and audit event |
| Expiry/Stock Rescue | Verify expired/expiring/safe grouping, FEFO ordering and the seven-day sales estimate; recommendations must not change prices or stock |
| Refund | Owner and CanRefundSales operator refund a completed receipt once; confirm external cash/card refund, require an open shift for cash, optionally restock original batches, and verify cash drawer/report totals |
| Cash reconciliation | Open a shift, complete cash sales and any cash refunds, close with actual counted cash, and verify variance plus audit event |
| Reports | Compare gross/net sales, refunds by method, waste, sellable inventory, low stock and cash variance against the underlying Neon records for the selected SAST date |
| Staff/RBAC | Grant and revoke each permission independently; verify navigation and server authorization both update; deactivate an operator and confirm refresh tokens are revoked |
| Audit/settings | Update business details and staff/product/stock operations; confirm owner-only audit history contains actor, timestamp and action |

The automated GitHub Actions workflow builds/tests the API and client and builds the frontend Docker image. It does not replace this live acceptance checklist or a browser-based test against Railway and Neon.
