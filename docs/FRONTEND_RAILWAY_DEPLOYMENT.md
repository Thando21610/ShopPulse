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
5. Generate a public domain in the service Networking settings.
6. Open the generated HTTPS domain and confirm the ShopPulse sign-in screen loads.

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

The repository now contains UI flows wired to the API for sign-in/catalogue, owner product management, inventory/batch/movement views, checkout with Rush Mode, cash-shift enforcement, external card-terminal confirmation and receipt printing, stock receipt, batch waste recording, expiry prioritisation and Stock Rescue recommendations, cash-shift opening/closing, owner financial summaries, staff management, audit history and business settings. The API persists these operations, applies tenant query filters, records stock movements/audit events, and enforces server-side policies. Card payments are recorded only after the operator confirms approval on a separate card terminal; no bank/card gateway is integrated. The frontend includes a single-flight refresh handler, HttpOnly refresh-cookie restoration, shared success/error toasts, mobile layouts, and category-colour product initials rather than hero/product stock imagery. The visual brief intentionally excludes hero and remote product photos; no stock imagery dependency is added.

Refund processing and anonymous queue management remain out of scope for this frontend phase. Reports do not include refund totals because no refund ledger is implemented. These are implemented code paths, not proof of successful production acceptance. Verify each journey against the deployed Railway API and Neon database using separate Owner and Operator accounts. In particular, confirm the refresh cookie is accepted by the browser across the deployed origins, validate that operators cannot access owner-only actions, and test concurrent checkout/stock updates before using the system for live trading.

A successful static frontend deployment does not by itself prove Neon connectivity or database readiness. Confirm API deployment logs and test the authenticated catalogue flow against the deployed API.
