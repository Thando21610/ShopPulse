#!/usr/bin/env sh
set -eu

ROOT_DIR="$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)"
cd "$ROOT_DIR"

DEPLOY_BRANCH="${1:-diagnostics}"

# Refuse accidental production deployment from this helper.
if [ "$DEPLOY_BRANCH" = "main" ] || [ "$DEPLOY_BRANCH" = "production" ]; then
    echo "ERROR: This helper is for diagnostic previews only. Production deployment requires a separate reviewed process."
    exit 1
fi

echo "==> Restoring ShopPulse"
dotnet restore ShopPulse.slnx

echo "==> Running release tests"
dotnet test ShopPulse.slnx --configuration Release --no-restore

echo "==> Publishing Blazor WebAssembly"
rm -rf output
dotnet publish src/ShopPulse.Client/ShopPulse.Client.csproj \
    --configuration Release \
    --no-restore \
    --output output

echo "==> Configuring the deployed client to use its same-origin Pages URL"
printf '{\n  "ApiBaseUrl": ""\n}\n' > output/wwwroot/appsettings.json

echo "==> Deploying Pages assets and Functions to branch: $DEPLOY_BRANCH"
npx --yes wrangler pages deploy output/wwwroot \
    --project-name shoppulse \
    --branch "$DEPLOY_BRANCH"

echo "==> ShopPulse diagnostic Pages deployment command completed"