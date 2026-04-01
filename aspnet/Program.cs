using aspnet.Data;
using aspnet.Queries;
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
