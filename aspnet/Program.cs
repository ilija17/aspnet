using aspnet.Data;
using aspnet.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<CasinoDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));

builder.Services.AddControllersWithViews();

// ── EF repositories (Lab 3) ──────────────────────────────────────────────────
builder.Services.AddScoped<ICasinoRepository,      CasinoEfRepository>();
builder.Services.AddScoped<IPlayerRepository,      PlayerEfRepository>();
builder.Services.AddScoped<ITableRepository,       TableEfRepository>();
builder.Services.AddScoped<IGameRepository,        GameEfRepository>();
builder.Services.AddScoped<IEmployeeRepository,    EmployeeEfRepository>();
builder.Services.AddScoped<IReservationRepository, ReservationEfRepository>();
builder.Services.AddScoped<ITransactionRepository, TransactionEfRepository>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseRouting();
app.UseAuthorization();
app.MapStaticAssets();
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}")
   .WithStaticAssets();

app.Run();
