# START HERE — Phone + Codespaces (fix structure)

Your GitHub repo must contain **folders** `src/` and `docs/`, not only loose files or a zip.

## A. Upload this zip to GitHub (phone OK)

1. Download **ShopPulse-CORRECT-STRUCTURE.zip** (from the chat / release).
2. Open https://github.com/Thando21610/ShopPulse
3. **Add file → Upload files** → select the zip only → Commit.

## B. Codespaces: extract and push

1. Repo → green **Code** → **Codespaces** → **Create codespace on main**.
2. In the terminal paste:

```bash
cd /workspaces/ShopPulse

find . -maxdepth 1 ! -name '.' ! -name '.git' ! -name 'ShopPulse-CORRECT-STRUCTURE.zip' -exec rm -rf {} + 2>/dev/null

unzip -o ShopPulse-CORRECT-STRUCTURE.zip -d /tmp/sp

if [ -d /tmp/sp/ShopPulse-clean ]; then
  cp -a /tmp/sp/ShopPulse-clean/. .
elif [ -d /tmp/sp/src ]; then
  cp -a /tmp/sp/. .
else
  cp -a /tmp/sp/. .
fi

rm -f ShopPulse-CORRECT-STRUCTURE.zip

echo "=== ROOT ===" && ls -la
echo "=== API ===" && ls src/ShopPulse.Api/

git add -A
git commit -m "Restore correct src/docs structure for Railway"
git push origin main
```

## C. Confirm

Open: https://github.com/Thando21610/ShopPulse/tree/main/src/ShopPulse.Api  

You must see `Program.cs`, `Dockerfile`, `Controllers/`.

## D. Railway

- New project from this GitHub repo  
- **Root Directory** = `src/ShopPulse.Api`  
- Variables: `UseSqlite=true`, `Jwt__SigningKey=<long secret>`  
- Generate public domain → test `/health`

Then set Client `wwwroot/appsettings.json` → `ApiBaseUrl` to that Railway URL.
