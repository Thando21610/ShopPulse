# ShopPulse

**Smart Retail Operations & Inventory Intelligence for Small Spaza Shops**  
Samsung Innovation Internship 2027 · Stage 1 Entry Assignment

> **ShopPulse — Smarter operations. Less waste. Better business.**

---

## 1. What this is

ShopPulse is a lightweight retail operations platform for small South African spaza shops.  
One completed sale updates the transaction, stock (FEFO), expiry exposure, daily financials and the audit trail — so the operator does not repeat work the system can do once.

| Item | Value |
|------|--------|
| **Pilot (Route A)** | Thandabantu Spaza Shop *(pending owner validation)* |
| **Currency** | South African Rand (ZAR) |
| **Frontend** | Blazor WebAssembly (.NET 10) |
| **Backend** | ASP.NET Core Web API (.NET 10) |
| **Database** | SQLite (local/demo) · Neon PostgreSQL (production) |
| **Auth** | JWT access token (15 min, in-memory) + HttpOnly rotating refresh cookie |
| **Deadline** | 30 November 2026 |

**Design principle:** a small tool one shop owner uses every day beats an impressive app nobody needs.

---

## 2. Repository structure (must look like this on GitHub)

```
ShopPulse/
├── README.md                 ← this file
├── START_HERE.md             ← phone / Codespaces restore steps
├── STRUCTURE.md              ← folder map
├── ShopPulse.slnx
├── .gitignore
├── vercel.json
├── docs/
│   ├── Complete_Design.html              ← full solution design (submission)
│   ├── PartII_Backend_Auth_UI.html       ← auth, tokens, UI feedback, tests
│   ├── ShopPulse_Solution_Design_Samsung_2027.html
│   └── DEPLOY_LIVE_URL.md                ← Railway + Vercel steps
└── src/
    ├── ShopPulse.Api/                    ← ASP.NET Core API
    │   ├── Program.cs
    │   ├── Dockerfile                    ← use this on Railway
    │   ├── ShopPulse.Api.csproj
    │   ├── appsettings.json
    │   ├── Controllers/
    │   │   ├── AuthController.cs
    │   │   └── ProductsController.cs
    │   ├── Auth/AuthService.cs
    │   ├── Data/AppDbContext.cs
    │   ├── Domain/Entities.cs
    │   └── Properties/
    └── ShopPulse.Client/                 ← Blazor WASM
        ├── Program.cs
        ├── ShopPulse.Client.csproj
        ├── Pages/
        ├── Layout/
        └── wwwroot/
            └── appsettings.json          ← set ApiBaseUrl after API is live
```

**If GitHub only shows loose `.cs` files in the root, the structure is wrong.**  
Restore with `START_HERE.md` (Codespaces + zip).

---

## 3. Test accounts (seeded automatically)

| Role | Email | Password |
|------|--------|----------|
| **Owner** | `owner@shoppulse.test` | `SyntheticPass!23` |
| **Operator** | `operator@shoppulse.test` | `SyntheticPass!23` |

No real personal data is used. All catalogue prices are synthetic placeholders.

---

## 4. Local run (computer / Codespaces)

```bash
# API
cd src/ShopPulse.Api
dotnet run
# Health: GET /health
# Login:  POST /api/auth/login  { "email":"owner@shoppulse.test", "password":"SyntheticPass!23" }

# Client (optional)
cd src/ShopPulse.Client
# Edit wwwroot/appsettings.json → "ApiBaseUrl": "http://localhost:PORT"
dotnet run
```

Default DB is **SQLite** (`UseSqlite=true`). For Neon, set `UseSqlite=false` and `ConnectionStrings__Default`.

---

## 5. Live deploy (summary)

Full phone-friendly steps: **`docs/DEPLOY_LIVE_URL.md`** and **`START_HERE.md`**.

| Piece | Host | Root / notes |
|--------|------|----------------|
| **API** | [Railway](https://railway.app) | Root Directory = `src/ShopPulse.Api` · uses `Dockerfile` |
| **Frontend** | [Vercel](https://vercel.com) or Cloudflare Pages | Publish client `wwwroot` after `dotnet publish` |

**API environment variables (Railway):**

- `UseSqlite` = `true` (demo)
- `Jwt__SigningKey` = long random secret (32+ characters)

After API is live, set in `src/ShopPulse.Client/wwwroot/appsettings.json`:

```json
{
  "ApiBaseUrl": "https://YOUR-API.up.railway.app"
}
```

Then publish and deploy the client.

**Verify API:** open `https://YOUR-API/health` → `{"status":"ok","service":"ShopPulse API"}`.

---

## 6. What is implemented vs next

### Implemented and tested

- Domain model (products, batches, sales-ready, waste, shifts, audit, refresh tokens, idempotency)
- JWT + rotating refresh cookie design (family reuse detection)
- Login / refresh / logout / me endpoints
- Products list (role-protected)
- Tenant isolation via `business_id` claim + query filters
- Seeded synthetic pilot data
- Dockerfile for container hosts
- Design docs for Samsung submission

### Still to build for a full demo

- Full sales/checkout with FEFO stock deduction + idempotency keys
- Stock receiving, waste register, cash shift close
- Blazor screens (Rush Mode, inventory, reports)
- Client `AuthMessageHandler` + single-flight silent refresh
- Production Neon connection (optional for Stage 1 demo)

---

## 7. Submission checklist (Stage 1)

- [ ] Publicly reachable live app URL (frontend)
- [ ] API health + login working
- [ ] Test accounts (table above)
- [ ] Code on GitHub with **correct folder structure** (`src/`, `docs/`)
- [ ] Progressive commits (not one bulk dump if possible)
- [ ] Walkthrough: video ≤ 5 min **or** document ≤ 2 pages
- [ ] No real customer personal data
- [ ] Owner permission before using real establishment name

**Design documents for markers:** open the HTML files under `docs/` in a browser (print-ready).

---

## 8. Verified API behaviour (Release build)

| Endpoint | Result |
|----------|--------|
| `GET /health` | `{"status":"ok"}` |
| `POST /api/auth/login` (valid) | 200 + access token + user |
| `POST /api/auth/login` (wrong password) | 401 `AUTH_INVALID_CREDENTIALS` |
| `GET /api/auth/me` + Bearer | 200 profile |
| `GET /api/products` + Bearer | 200, 8 seeded products with stock |

Build: `dotnet build src/ShopPulse.Api -c Release` → **0 errors, 0 warnings**.

---

## 9. Important rules (Samsung)

- Problem statements remain **hypotheses** until owner interview confirms them.
- Free-tier only for the assessable deployment.
- Own work; synthetic data only.
- Permissions enforced on the **server**, not only by hiding UI buttons.

---

## 10. Links

| Resource | Path / URL |
|----------|------------|
| Full design | `docs/Complete_Design.html` |
| Auth & UI feedback spec | `docs/PartII_Backend_Auth_UI.html` |
| Deploy guide | `docs/DEPLOY_LIVE_URL.md` |
| Phone restore | `START_HERE.md` |
| GitHub | https://github.com/Thando21610/ShopPulse |

---

ShopPulse · Samsung Innovation Internship 2027 · ZAR · POPIA-conscious (no customer personal data in the demo)
