using aspnet.Models;
using Microsoft.EntityFrameworkCore;

namespace aspnet.Data;

public class CasinoDbContext : DbContext
{
    public CasinoDbContext(DbContextOptions<CasinoDbContext> options) : base(options) { }

    public DbSet<Casino> Casinos { get; set; }
    public DbSet<Game> Games { get; set; }
    public DbSet<Table> Tables { get; set; }
    public DbSet<Player> Players { get; set; }
    public DbSet<Employee> Employees { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<Reservation> Reservations { get; set; }
}
