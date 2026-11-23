using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ShopPulse.Api.Auth;
using ShopPulse.Api.Authorization;
using ShopPulse.Api.Data;
using ShopPulse.Api.Domain;

var builder = WebApplication.CreateBuilder(args);

// Cloud hosts (Railway, Render, Cloud Run) inject PORT
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

// ─── Configuration ───────────────────────────────────────────────────────────
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["SigningKey"];
if (string.IsNullOrWhiteSpace(jwtKey))
{
    if (!builder.Environment.IsDevelopment())
        throw new InvalidOperationException("Jwt:SigningKey must be configured outside Development.");
    jwtKey = "LOCAL-ONLY-ShopPulse-Development-Key-Change-Before-Deploy-2026";
}
if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
    throw new InvalidOperationException("Jwt:SigningKey must contain at least 32 UTF-8 bytes.");
var jwtIssuer = jwtSection["Issuer"] ?? "shoppulse-api";
var jwtAudience = jwtSection["Audience"] ?? "shoppulse-app";

// ─── Database ────────────────────────────────────────────────────────────────
// Prefer Neon / PostgreSQL connection string; fall back to SQLite for local demo
var conn = builder.Configuration.GetConnectionString("Default");
if (string.IsNullOrWhiteSpace(conn))
{
    if (!builder.Environment.IsDevelopment())
        throw new InvalidOperationException("ConnectionStrings:Default must be configured for PostgreSQL outside Development.");
    conn = "Host=localhost;Database=shoppulse;Username=postgres;Password=postgres";
}

var useSqlite = builder.Configuration.GetValue<bool?>("UseSqlite")
    ?? builder.Environment.IsDevelopment();

if (!builder.Environment.IsDevelopment() && useSqlite)
    throw new InvalidOperationException("SQLite is disabled outside Development. Configure UseSqlite=false and a PostgreSQL connection string.");

if (!useSqlite && conn.Contains("Data Source=", StringComparison.OrdinalIgnoreCase))
    throw new InvalidOperationException("A SQLite connection string cannot be used when PostgreSQL is selected.");

if (useSqlite)
{
    builder.Services.AddDbContext<AppDbContext>(opt =>
        opt.UseSqlite(builder.Configuration.GetConnectionString("Sqlite")
            ?? "Data Source=shoppulse.db"));
}
else
{
    builder.Services.AddDbContext<AppDbContext>(opt =>
        opt.UseNpgsql(conn));
}

// ─── Auth ────────────────────────────────────────────────────────────────────
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentBusiness>(sp =>
{
    var accessor = sp.GetRequiredService<IHttpContextAccessor>();
    var user = accessor.HttpContext?.User;
    if (user?.Identity?.IsAuthenticated != true)
        return new NullCurrentBusiness();
    try { return new CurrentBusiness(accessor); }
    catch { return new NullCurrentBusiness(); }
});
builder.Services.AddScoped<AuthService>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromSeconds(60),
            NameClaimType = "name",
            RoleClaimType = "role",
        };
        options.Events = new JwtBearerEvents
        {
            OnChallenge = async ctx =>
            {
                ctx.HandleResponse();
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                ctx.Response.ContentType = "application/problem+json";
                var expired = ctx.AuthenticateFailure is SecurityTokenExpiredException;
                await ctx.Response.WriteAsJsonAsync(new
                {
                    type = "https://httpstatuses.io/401",
                    title = "Not authenticated",
                    status = 401,
                    detail = "Your session has expired.",
                    code = expired ? "AUTH_TOKEN_EXPIRED" : "AUTH_FORBIDDEN",
                    correlationId = ctx.HttpContext.TraceIdentifier
                });
            }
        };
    });

builder.Services.AddScoped<IAuthorizationHandler, CanAdjustStockHandler>();
builder.Services.AddScoped<IAuthorizationHandler, CanRefundSalesHandler>();
builder.Services.AddScoped<IAuthorizationHandler, CanReceiveStockHandler>();
builder.Services.AddScoped<IAuthorizationHandler, CanManageProductsHandler>();
builder.Services.AddAuthorization(ShopPulsePolicies.AddPolicies);

// ─── Rate limiting ───────────────────────────────────────────────────────────
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("auth", limiter =>
    {
        limiter.PermitLimit = 10;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
    });
    options.OnRejected = async (ctx, ct) =>
    {
        ctx.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await ctx.HttpContext.Response.WriteAsJsonAsync(new
        {
            type = "https://httpstatuses.io/429",
            title = "Too many requests",
            status = 429,
            detail = "Please wait a moment before trying again.",
            code = "RATE_LIMITED",
            correlationId = ctx.HttpContext.TraceIdentifier
        }, ct);
    };
});

// Railway terminates public TLS at its edge and forwards the original scheme.
// Railway overwrites these headers on public ingress; trust only services in this
// Railway environment and keep the forwarded-header hop limit to one.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.ForwardedForHeaderName = "X-Real-IP";
    options.ForwardLimit = 1;
    if (!builder.Environment.IsDevelopment())
    {
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
    }
});

builder.Services.AddControllers();
builder.Services.AddOpenApi();
// CORS: explicit origins only. Configure Cors__Origins on the hosting platform.
var corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
    ?? (builder.Environment.IsDevelopment() ? new[] { "http://localhost:5000", "https://localhost:5001" } : Array.Empty<string>());
if (corsOrigins.Length == 0)
    throw new InvalidOperationException("Configure Cors:Origins with the exact deployed frontend origin.");

if (!builder.Environment.IsDevelopment()
    && corsOrigins.Any(origin =>
        !Uri.TryCreate(origin, UriKind.Absolute, out var parsed)
        || parsed.Scheme != Uri.UriSchemeHttps
        || parsed.IsLoopback))
{
    throw new InvalidOperationException(
        "Production Cors:Origins must contain only absolute HTTPS origins and must not include localhost.");
}
builder.Services.AddCors(o => o.AddPolicy("frontend", policy =>
{
    policy.WithOrigins(corsOrigins)
          .AllowAnyHeader()
          .AllowAnyMethod()
          .AllowCredentials();
}));

var app = builder.Build();

// ─── Database startup ─────────────────────────────────────────────────────────
// Development uses EnsureCreated + synthetic seed data for local demos only.
// Production must have reviewed EF Core migrations and never receives demo users/data.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    if (app.Environment.IsDevelopment())
    {
        await db.Database.EnsureCreatedAsync();
        await SeedAsync(db);
    }
    else
    {
        var migrations = db.Database.GetMigrations().ToArray();
        if (migrations.Length == 0)
        {
            throw new InvalidOperationException(
                "Production startup is blocked: no EF Core migrations are present. " +
                "Create and review an initial migration, then deploy it before enabling production.");
        }

        await db.Database.MigrateAsync();
    }
}

// Expose the built-in OpenAPI document in all environments for deployment verification.
app.MapOpenApi();

app.UseForwardedHeaders();
app.UseRateLimiter();
app.UseCors("frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "ShopPulse API" }));
app.MapGet("/ready", async (AppDbContext db, CancellationToken ct) =>
{
    try
    {
        return await db.Database.CanConnectAsync(ct)
            ? Results.Ok(new { status = "ready", database = "reachable" })
            : Results.Json(new { status = "not_ready", database = "unreachable" }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
    catch
    {
        return Results.Json(new { status = "not_ready", database = "unreachable" }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});

app.Run();

static async Task SeedAsync(AppDbContext db)
{
    if (await db.Businesses.AnyAsync()) return;

    var businessId = Guid.Parse("b3a2c7d1-0001-4000-8000-000000000001");
    var ownerId = Guid.Parse("6c1f9a2e-0001-4000-8000-000000000001");
    var opId = Guid.Parse("6c1f9a2e-0002-4000-8000-000000000002");

    var business = new Business
    {
        Id = businessId,
        Name = "Synthetic Pilot Shop",
        Address = "Demo Street, South Africa"
    };
    db.Businesses.Add(business);

    // Password: SyntheticPass!23
    var hash = BCrypt.Net.BCrypt.HashPassword("SyntheticPass!23");
    db.Users.AddRange(
        new AppUser
        {
            Id = ownerId,
            BusinessId = businessId,
            Email = "owner@shoppulse.test",
            PasswordHash = hash,
            DisplayName = "Owner One",
            Role = "Owner",
            CanAdjustStock = true
        },
        new AppUser
        {
            Id = opId,
            BusinessId = businessId,
            Email = "operator@shoppulse.test",
            PasswordHash = hash,
            DisplayName = "Operator Two",
            Role = "Operator",
            CanAdjustStock = true
        });

    var products = new[]
    {
        ("BRD-001", "Bread", 15.00m, true),
        ("MLK-001", "Milk", 18.00m, true),
        ("SGR-001", "Sugar", 22.00m, false),
        ("RCE-001", "Rice", 28.00m, false),
        ("EGG-001", "Eggs", 45.00m, true),
        ("OIL-001", "Cooking oil", 35.00m, false),
        ("SOAP-01", "Soap", 12.00m, false),
        ("MZM-001", "Maize meal", 32.00m, false),
    };

    foreach (var (sku, name, price, expiry) in products)
    {
        var pid = Guid.NewGuid();
        db.Products.Add(new Product
        {
            Id = pid,
            BusinessId = businessId,
            Sku = sku,
            Name = name,
            UnitPrice = price,
            MinimumStockLevel = 5,
            HasExpiry = expiry,
            IsActive = true
        });
        db.StockBatches.Add(new StockBatch
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            ProductId = pid,
            BatchReference = $"B-{sku}-DEMO",
            ReceivedQuantity = 40,
            RemainingQuantity = 40,
            UnitCost = price * 0.6m,
            ExpiryDate = expiry ? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)) : null,
            ReceivedAtUtc = DateTimeOffset.UtcNow.AddDays(-1)
        });
    }

    await db.SaveChangesAsync();
}
