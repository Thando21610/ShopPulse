# Repository structure

```
ShopPulse/
├── README.md
├── START_HERE.md
├── STRUCTURE.md
├── ShopPulse.slnx
├── .gitignore
├── vercel.json
├── docs/
│   ├── Complete_Design.html
│   ├── PartII_Backend_Auth_UI.html
│   ├── ShopPulse_Solution_Design_Samsung_2027.html
│   └── DEPLOY_LIVE_URL.md
└── src/
    ├── ShopPulse.Api/
    │   ├── Program.cs
    │   ├── Dockerfile
    │   ├── ShopPulse.Api.csproj
    │   ├── appsettings.json
    │   ├── Controllers/
    │   ├── Auth/
    │   ├── Data/
    │   ├── Domain/
    │   └── Properties/
    └── ShopPulse.Client/
        ├── Program.cs
        ├── ShopPulse.Client.csproj
        ├── Pages/
        ├── Layout/
        └── wwwroot/appsettings.json
```

Railway **Root Directory** must be: `src/ShopPulse.Api`
