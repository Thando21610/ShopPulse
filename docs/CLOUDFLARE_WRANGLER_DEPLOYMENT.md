# ShopPulse Cloudflare Pages deployment with Wrangler

The Blazor WebAssembly client is published as static assets. Cloudflare Pages Functions proxy browser API traffic to the Railway API so the browser uses one origin for both the application and its authentication cookie.

## Before using the checked-in Wrangler configuration

The first time you use Wrangler against the existing Pages project, download its current configuration:

```sh
wrangler pages download config shoppulse
```

Cloudflare documents that this command generates a Wrangler configuration from the current Pages project settings. Review the downloaded file before deploying. Preserve any existing compatibility settings, bindings, and environment configuration, then merge the ShopPulse values:

- `name = "shoppulse-bpd"`
- `pages_build_output_dir = "./output/wwwroot"`
- `API_ORIGIN = "https://shoppulse-production-e6673892.up.railway.app"`

A Wrangler configuration file becomes the source of truth for the Pages settings it defines. Do not replace existing project settings with a hand-written minimal file without checking the downloaded configuration first.

## Preview deployment

From the repository root, after checking out the authentication fix branch:

```sh
sh deploy-pages.sh
```

The script restores the solution, runs Release tests, publishes the Blazor client to `output`, sets the published app's API base URL to the current Pages origin, and uploads the static assets plus the root `functions/` directory.

By default, it deploys to the existing non-production `diagnostics` branch. The default target is the existing diagnostics branch hostname:

```
https://diagnostics.shoppulse-bpd.pages.dev
```

Before testing login on that preview, add that exact HTTPS origin to the Railway API's `Cors:Origins` configuration. Keep the production origin in the allowlist as well. Do not use a wildcard origin; the API also validates the browser Origin header on login, refresh, and logout.

## Production deployment

Only after the preview passes the browser acceptance checks and the change is approved, deploy the production branch explicitly:

```sh
sh deploy-pages.sh main
```

This is a production deployment. Do not run it until the PR has been reviewed and merged and the production API allowlist has been verified.

## Browser acceptance checks

1. Open the deployed Pages origin and sign in.
2. Confirm the browser receives the `sp_rt` refresh cookie as HttpOnly and Secure, with path `/api/auth`.
3. Reload the page; the authenticated session should restore without returning to sign-in.
4. Wait for an access-token refresh and confirm the session remains active.
5. Sign out; reload and confirm the session is no longer authenticated.
6. Verify a user from one business cannot access another business's data.

The Pages Functions forward the original browser Origin and Cookie headers to Railway. The API's explicit `Cors:Origins` allowlist must contain the exact Pages origin being tested. A successful deployment or health check alone does not prove cookie persistence.
