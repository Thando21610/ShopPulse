#!/usr/bin/env sh
set -eu

ROOT_DIR="$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)"
cd "$ROOT_DIR"

DEPLOY_BRANCH="${1:-diagnostics}"

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
wrangler pages deploy output/wwwroot \
    --project-name shoppulse \
    --branch "$DEPLOY_BRANCH"

echo "==> ShopPulse Pages deployment command completed"
