# Get a live public URL (mobile-friendly steps)

You need **two** public URLs:
1. **Frontend** (Blazor WASM) → Vercel or Cloudflare Pages (easy on phone)
2. **API** → Railway or Render (supports .NET)

## 1. Put the code on GitHub (required for free deploys)

1. On your phone or computer open [github.com](https://github.com) and sign in.
2. Create a **new private repository** named `ShopPulse`.
3. Upload the contents of the `ShopPulse` folder (or push the zip after extracting).
   - On phone: use the GitHub mobile app or the website “Upload files”.
   - Or from a computer: `git push` after adding the remote.

## 2. Deploy the API (live backend URL)

### Option A — Railway (recommended, simple)

1. Go to [railway.app](https://railway.app) → Sign up with GitHub.
2. **New Project** → **Deploy from GitHub repo** → select `ShopPulse`.
3. Set the **Root Directory** to `src/ShopPulse.Api`.
4. Railway will detect the Dockerfile.
5. Add variables (optional for demo):
   - `UseSqlite` = `true` (keeps SQLite file; data resets on redeploy — fine for demo)
   - For real Neon later: set `UseSqlite=false` and `ConnectionStrings__Default` = your Neon connection string
   - `Jwt__SigningKey` = a long random secret (min 32 characters)
6. Deploy. Railway gives you a public URL like:
   `https://shoppulse-api-production-xxxx.up.railway.app`
7. Test: open `https://YOUR-URL/health` — should return `{"status":"ok"}`.

### Option B — Render

1. [render.com](https://render.com) → New → Web Service → connect GitHub.
2. Root: `src/ShopPulse.Api`, Environment: Docker.
3. Same env vars as above.

## 3. Deploy the frontend (live app URL)

### Vercel (very easy on mobile)

1. Publish the client first (on a computer or Codespace):

```bash
cd src/ShopPulse.Client
dotnet publish -c Release -o ../../publish
```

2. Edit `publish/wwwroot/appsettings.json` (or the one we put in wwwroot) and set:

```json
{
  "ApiBaseUrl": "https://YOUR-RAILWAY-API-URL"
}
```

3. Go to [vercel.com](https://vercel.com) → Sign up with GitHub.
4. **Add New Project** → Import the repo **or** drag-and-drop the `publish/wwwroot` folder.
5. Framework: **Other**. Output directory: leave default or set to the wwwroot contents.
6. Deploy. You get a URL like:
   `https://shoppulse-xxx.vercel.app`

### Cloudflare Pages (matches the design doc)

1. [dash.cloudflare.com](https://dash.cloudflare.com) → Pages → Create project.
2. Connect GitHub or upload the static `wwwroot` files.
3. Same: set `ApiBaseUrl` to your API URL before upload.

## 4. Wire the client to the API

The Blazor client must call your live API. After you have the Railway/Render URL:

- Update `wwwroot/appsettings.json` → `"ApiBaseUrl": "https://your-api..."`
- Rebuild/republish the client and redeploy to Vercel/Pages.

Also add your Vercel URL to the API’s CORS allowed origins (or set a permissive list for the demo).

## 5. Test accounts (seeded)

| Role     | Email                  | Password         |
|----------|------------------------|------------------|
| Owner    | owner@shoppulse.test   | SyntheticPass!23 |
| Operator | operator@shoppulse.test| SyntheticPass!23 |

## 6. What to submit to Samsung

- Live frontend URL (Vercel or Pages)
- Note that test accounts are above
- Private GitHub repo link
- The design HTML docs already in `/docs`

## Mobile tip

You can do GitHub upload + Vercel + Railway entirely from a phone browser. The only step that is easier on a computer is `dotnet publish`. If you have no computer:

- Use GitHub Codespaces (free tier) from the phone browser to run `dotnet publish`, then download the `publish/wwwroot` folder and upload to Vercel.
