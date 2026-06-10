using System.Security.Claims;
using System.Text.Encodings.Web;
using aspnet.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace aspnet.Tests;

/// <summary>
/// Pokreće stvarnu aplikaciju s InMemory bazom (jedinstvenom po factory instanci)
/// i test autentikacijskom shemom upravljanom preko "X-Test-Role" headera.
/// </summary>
public class CasinoApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"CasinoTests-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "TestConnection"
            });
        });

        builder.ConfigureServices(services =>
        {
            // SQL Server registraciju zamijeniti InMemory bazom
            services.RemoveAll(typeof(DbContextOptions<CasinoDbContext>));
            services.RemoveAll(typeof(IDbContextOptionsConfiguration<CasinoDbContext>));

            services.AddDbContext<CasinoDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));

            // Test autentikacija umjesto Identity cookieja
            services.AddAuthentication()
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });

            services.PostConfigure<AuthenticationOptions>(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
            });
        });
    }
}

/// <summary>
/// Autenticira zahtjev samo ako sadrži "X-Test-Role" header; bez headera vraća 401.
/// </summary>
public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Test";
    public const string RoleHeader = "X-Test-Role";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(RoleHeader, out var role) || string.IsNullOrEmpty(role))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "test-user"),
            new Claim(ClaimTypes.Role, role.ToString())
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
