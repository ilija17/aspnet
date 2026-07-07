using aspnet.Data;
using aspnet.Mcp;
using aspnet.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

var builder = Host.CreateApplicationBuilder(args);

var connectionString = builder.Configuration["ConnectionStrings:DefaultConnection"]
    ?? Environment.GetEnvironmentVariable("CONNECTIONSTRINGS__DEFAULTCONNECTION");

ArgumentNullException.ThrowIfNullOrEmpty(connectionString, "Connection string not configured. Set 'ConnectionStrings:DefaultConnection' in config or CONNECTIONSTRINGS__DEFAULTCONNECTION env var.");

builder.Services.AddDbContext<CasinoDbContext>(options =>
    options.UseSqlServer(connectionString, o =>
    {
        o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
        o.EnableRetryOnFailure();
    }));

builder.Services.AddScoped<ICasinoRepository, CasinoEfRepository>();
builder.Services.AddScoped<IPlayerRepository, PlayerEfRepository>();
builder.Services.AddScoped<ITableRepository, TableEfRepository>();
builder.Services.AddScoped<IGameRepository, GameEfRepository>();
builder.Services.AddScoped<IEmployeeRepository, EmployeeEfRepository>();
builder.Services.AddScoped<IReservationRepository, ReservationEfRepository>();
builder.Services.AddScoped<ITransactionRepository, TransactionEfRepository>();

builder.Services.AddScoped<CasinoTools>();

builder.Services.AddMcpServer()
    .WithTools<CasinoTools>()
    .WithStdioServerTransport();

builder.Logging.ClearProviders();

var host = builder.Build();

var db = host.Services.GetRequiredService<CasinoDbContext>();
if (db.Database.IsSqlServer())
{
    try { db.Database.Migrate(); }
    catch (Exception ex)
    {
        var logger = host.Services.GetRequiredService<ILogger<Program>>();
        logger.LogWarning(ex, "Database migration skipped");
    }
}

await host.RunAsync();
