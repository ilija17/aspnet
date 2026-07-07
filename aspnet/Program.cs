using aspnet.Data;
using aspnet.Logging;
using aspnet.Mcp;
using aspnet.Middleware;
using aspnet.Models;
using aspnet.Repositories;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;
using Serilog;

// Učitaj .env prije buildanja konfiguracije (lokalni razvoj; u containeru
// se varijable postavljaju kroz compose). Postojeće env varijable imaju prednost.
var envFile = Path.Combine(Directory.GetCurrentDirectory(), ".env");
if (File.Exists(envFile))
{
    foreach (var line in File.ReadAllLines(envFile))
    {
        var trimmed = line.Trim();
        if (trimmed.Length == 0 || trimmed.StartsWith('#')) continue;
        var idx = trimmed.IndexOf('=');
        if (idx <= 0) continue;
        var key = trimmed[..idx].Trim();
        var value = trimmed[(idx + 1)..].Trim().Trim('"');
        if (Environment.GetEnvironmentVariable(key) is null)
        {
            Environment.SetEnvironmentVariable(key, value);
        }
    }
}

var builder = WebApplication.CreateBuilder(args);

// Ključeve čuvamo u "keys" mapi relativnoj na content root. U Dockeru je
// content root /app, pa ovo ostaje /app/keys (named volume + chown iz
// Dockerfilea). Lokalno (dotnet run) to je .../aspnet/keys, koji je zapisiv —
// hardkodirani /app nije postojao niti se mogao stvoriti pa je padala zaštita
// (antiforgery/DataProtection) na lokalnom pokretanju.
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "keys")));

builder.Services.AddDbContext<CasinoDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        o =>
        {
            o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            // SQL Server u containeru može još podizati dok aplikacija krene
            o.EnableRetryOnFailure();
        }));

// ── Identity (Lab 5) ─────────────────────────────────────────────────────────
builder.Services
    .AddIdentity<AppUser, IdentityRole>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequireNonAlphanumeric = false;
    })
    .AddEntityFrameworkStores<CasinoDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";

    // API klijenti očekuju status kodove, ne redirect na login stranicu
    options.Events.OnRedirectToLogin = ctx =>
    {
        if (ctx.Request.Path.StartsWithSegments("/api"))
        {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
        }
        else
        {
            ctx.Response.Redirect(ctx.RedirectUri);
        }
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = ctx =>
    {
        if (ctx.Request.Path.StartsWithSegments("/api"))
        {
            ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
        }
        else
        {
            ctx.Response.Redirect(ctx.RedirectUri);
        }
        return Task.CompletedTask;
    };
});

// ── Google OAuth (Lab 5) — registrira se samo ako su secrets konfigurirani ──
// Compose mapira GOOGLE_CLIENT_ID → Authentication__Google__ClientId, ali kod
// lokalnog pokretanja .env postavlja samo sirova imena pa čitamo i njih
var googleClientId = builder.Configuration["Authentication:Google:ClientId"]
    ?? builder.Configuration["GOOGLE_CLIENT_ID"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]
    ?? builder.Configuration["GOOGLE_CLIENT_SECRET"];

if (!string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret))
{
    builder.Services
        .AddAuthentication()
        .AddGoogle(options =>
        {
            options.ClientId = googleClientId;
            options.ClientSecret = googleClientSecret;
        });
}

// ── DeepSeek AI chat ─────────────────────────────────────────────────────────
builder.Services.AddHttpClient();
builder.Services.AddScoped<aspnet.Services.ChatToolService>();

// ── Waitlist mail (SMTP preko MailKita; host/port u appsettings, creds u .env) ─
builder.Services.AddScoped<aspnet.Services.MailService>();

// ── Blackjack i rulet — stanje igre u memoriji po igraču, zato singletoni;
// novac ide kroz bazu (Player.Balance + Bet/Win transakcije) ────────────────
builder.Services.AddSingleton<aspnet.Services.BlackjackGameService>();
builder.Services.AddSingleton<aspnet.Services.RouletteGameService>();
builder.Services.AddSingleton<aspnet.Services.ThreeBodyGameService>();
builder.Services.AddSingleton<aspnet.Services.SlotGameService>();

builder.Services.AddControllersWithViews()
    .AddJsonOptions(o => o.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles);

// ── EF repositories (Lab 3) ──────────────────────────────────────────────────
builder.Services.AddScoped<ICasinoRepository,      CasinoEfRepository>();
builder.Services.AddScoped<IPlayerRepository,      PlayerEfRepository>();
builder.Services.AddScoped<ITableRepository,       TableEfRepository>();
builder.Services.AddScoped<IGameRepository,        GameEfRepository>();
builder.Services.AddScoped<IEmployeeRepository,    EmployeeEfRepository>();
builder.Services.AddScoped<IReservationRepository, ReservationEfRepository>();
builder.Services.AddScoped<ITransactionRepository, TransactionEfRepository>();
builder.Services.AddScoped<CasinoTools>();

// ── MCP server (Model Context Protocol) — HTTP transport, network-accessible ──
builder.Services.AddMcpServer()
    .WithTools<CasinoTools>()
    .WithHttpTransport(options => options.Stateless = false);

var logBufferSink = new LogBufferSink(maxEntries: 2000);
builder.Services.AddSingleton(logBufferSink);

builder.Host.UseSerilog((ctx, cfg) =>
{
    cfg.ReadFrom.Configuration(ctx.Configuration);
    cfg.WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}");
    cfg.WriteTo.Sink(logBufferSink);
});

var app = builder.Build();

// Iza nginx reverse proxyja: bez ovoga aplikacija misli da je shema http
// pa OAuth redirect URI i secure cookieji ne rade ispravno
var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};
// U containeru zahtjevi od nginxa ne dolaze s loopbacka, pa default
// lista vjerovanih proxyja (samo loopback) ne vrijedi
forwardedHeadersOptions.KnownNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeadersOptions);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// MapStaticAssets ne poslužuje datoteke uploadane za vrijeme rada aplikacije
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<RequestLoggingMiddleware>();
app.MapMcp("/mcp");

app.MapGet("/mcp-info", () => Results.Content($$"""
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <title>MCP Server — Casino Management</title>
    <style>
        * { margin:0; padding:0; box-sizing:border-box; }
        body { font-family: system-ui, -apple-system, sans-serif; background:#0f172a; color:#e2e8f0; padding:2rem; min-height:100vh; }
        .container { max-width:800px; margin:0 auto; }
        h1 { font-size:1.75rem; margin-bottom:.25rem; color:#f8fafc; }
        .subtitle { color:#94a3b8; margin-bottom:2rem; }
        .status { display:inline-flex; align-items:center; gap:.5rem; background:#064e3b; color:#6ee7b7; padding:.35rem .75rem; border-radius:6px; font-size:.85rem; font-weight:600; margin-bottom:2rem; }
        .status::before { content:''; width:8px; height:8px; border-radius:50%; background:#10b981; animation:pulse 2s infinite; }
        @keyframes pulse { 0%,100%{opacity:1} 50%{opacity:.4} }
        .card { background:#1e293b; border-radius:8px; padding:1.25rem; margin-bottom:1rem; border:1px solid #334155; }
        .card h3 { font-size:1rem; margin-bottom:.35rem; color:#f1f5f9; }
        .card .desc { color:#94a3b8; font-size:.875rem; line-height:1.5; }
        .card .meta { display:flex; gap:1rem; margin-top:.5rem; font-size:.8rem; color:#64748b; }
        .section-title { font-size:1.1rem; color:#cbd5e1; margin:2rem 0 1rem; padding-bottom:.5rem; border-bottom:1px solid #334155; }
        .endpoint { font-family:monospace; background:#0f172a; padding:.15rem .4rem; border-radius:4px; color:#7dd3fc; font-size:.85rem; }
        .row { display:flex; justify-content:space-between; align-items:center; flex-wrap:wrap; gap:.5rem; }
        .btn { display:inline-block; background:#2563eb; color:white; text-decoration:none; padding:.4rem .75rem; border-radius:6px; font-size:.8rem; font-weight:600; transition:background .15s; border:none; cursor:pointer; }
        .btn:hover { background:#1d4ed8; }
        details { margin-top:.75rem; }
        summary { color:#94a3b8; cursor:pointer; font-size:.8rem; }
        pre { background:#0f172a; padding:.75rem; border-radius:6px; overflow-x:auto; margin-top:.5rem; font-size:.8rem; line-height:1.5; color:#a5b4fc; }
    </style>
</head>
<body>
<div class="container">
    <h1>🔌 MCP Server — Casino Management</h1>
    <p class="subtitle">Model Context Protocol · HTTP Transport · Session-based</p>

    <div class="status">Online</div>

    <div class="card">
        <h3>Endpoint</h3>
        <p class="desc">
            <span class="endpoint">POST /mcp</span> — JSON-RPC messages (initialize, tools/list, tools/call)<br/>
            <span class="endpoint">GET  /mcp</span> — SSE event stream (requires <code>Mcp-Session-Id</code> header)
        </p>
    </div>

    <div class="card">
        <h3>How to connect</h3>
        <p class="desc">
            This page is a read-only diagnostic. The MCP protocol works over JSON-RPC — use an MCP client:
        </p>
        <details>
            <summary>Claude Code / VS Code config</summary>
<pre>{
  "mcpServers": {
    "casino": {
      "type": "sse",
      "url": "http://localhost:5050/mcp"
    }
  }
}</pre>
        </details>
        <details>
            <summary>opencode.json</summary>
<pre>{
  "mcp": {
    "casino": {
      "type": "http",
      "url": "http://localhost:5050/mcp",
      "enabled": true
    }
  }
}</pre>
        </details>
    </div>

    <h2 class="section-title">🛠 Available Tools</h2>

    <div class="card">
        <h3>search_all</h3>
        <p class="desc">Searches across all entities (Casinos, Players, Games, Tables, Employees, Reservations, Transactions) and returns matching results grouped by type.</p>
        <div class="meta"><span>Query:</span><span>string q, int limit = 5</span></div>
    </div>
    <div class="card">
        <h3>get_entity_counts</h3>
        <p class="desc">Returns a summary of database entity counts (row counts for each table).</p>
        <div class="meta"><span>Parameters:</span><span>none</span></div>
    </div>
    <div class="card">
        <h3>list_casinos</h3>
        <p class="desc">Returns a list of all casinos with their IDs, names, and addresses.</p>
        <div class="meta"><span>Parameters:</span><span>none</span></div>
    </div>
    <div class="card">
        <h3>list_players</h3>
        <p class="desc">Returns a list of all players with their IDs, names, emails, and balances.</p>
        <div class="meta"><span>Parameters:</span><span>none</span></div>
    </div>
    <div class="card">
        <h3>get_table_availability</h3>
        <p class="desc">Returns all tables grouped by casino, showing which are available.</p>
        <div class="meta"><span>Parameters:</span><span>none</span></div>
    </div>
    <div class="card">
        <h3>get_database_schema</h3>
        <p class="desc">Returns the database schema and application route map for the Casino Management System.</p>
        <div class="meta"><span>Parameters:</span><span>none</span></div>
    </div>

    <p style="color:#475569;font-size:.75rem;margin-top:3rem;">
        Diagnostics page · <a href="/" style="color:#64748b;">← back to app</a>
    </p>
</div>
</body>
</html>
""", "text/html"));

if (app.Environment.IsEnvironment("Testing"))
{
    // WebApplicationFactory nema static assets manifest pa MapStaticAssets ne radi
    app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
}
else
{
    app.MapStaticAssets();
    app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}")
       .WithStaticAssets();
}

// Migracije + seed rola i administratorskog korisnika
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<CasinoDbContext>();
    if (dbContext.Database.IsSqlServer())
    {
        // Idempotentno: primjenjuje samo migracije koje nedostaju
        dbContext.Database.Migrate();
    }

    await IdentitySeed.SeedAsync(scope.ServiceProvider);
}

app.Run();

// Omogućuje WebApplicationFactory<Program> u integracijskim testovima
public partial class Program { }
