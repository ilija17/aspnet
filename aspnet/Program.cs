using aspnet.Data;
using aspnet.Queries;
using aspnet.Repositories;
using Microsoft.EntityFrameworkCore;

// Build in-memory seed data (casinos, players, reservations …)
var seed = SeedData.Create();

// Run all LINQ queries and print results to the console
CasinoQueries.RunAndPrint(seed);

// ── ASP.NET host setup ────────────────────────────────────────────────────────
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<CasinoDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllersWithViews();

// ── Mock repositories (Lab 2) ─────────────────────────────────────────────────
builder.Services.AddSingleton<ICasinoRepository,     CasinoMockRepository>();
builder.Services.AddSingleton<IPlayerRepository,     PlayerMockRepository>();
builder.Services.AddSingleton<ITableRepository,      TableMockRepository>();
builder.Services.AddSingleton<IGameRepository,       GameMockRepository>();
builder.Services.AddSingleton<IEmployeeRepository,   EmployeeMockRepository>();
builder.Services.AddSingleton<IReservationRepository, ReservationMockRepository>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();
app.MapStaticAssets();
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}")
   .WithStaticAssets();

app.Run();
