using aspnet.Data;
using aspnet.Models;
using aspnet.Repositories;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

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

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("/app/keys"));

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
