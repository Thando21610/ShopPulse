# ShopPulse frontend deployment (Railway)

## What is included

The Blazor WebAssembly client is published as static files and served by Nginx. The Dockerfile and Nginx configuration live in this directory so Railway can build the frontend with this service root:

`src/ShopPulse.Client`

The client reads `ApiBaseUrl` from `wwwroot/appsettings.json`. The committed value currently targets the supplied ShopPulse Railway API origin. Change it to the actual API origin if that domain changes; do not include an API route suffix.

## Railway frontend service

1. Create a new service from the `Thando21610/ShopPulse` GitHub repository.
2. Set the service Root Directory to `/src/ShopPulse.Client`.
3. Set Builder to Dockerfile if Railway does not detect it automatically.
4. Deploy and wait for the image build and deployment to complete.
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

## Current feature boundary

The deployed client currently implements sign-in and the live product catalogue. Sales/checkout, stock receiving, waste, cash reconciliation and reporting are not implemented end-to-end yet. Do not expose those as working navigation/actions until the API contracts, authorization, persistence, UI and tests exist.

The access token is currently held in memory by the Blazor client. A browser reload clears it; refresh-cookie/session restoration across the separate frontend and API origins remains unfinished and must be tested before a production POS rollout.

A successful static frontend deployment does not by itself prove Neon connectivity or database readiness. Confirm API deployment logs and test the authenticated catalogue flow against the deployed API.
