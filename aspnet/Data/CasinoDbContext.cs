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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Games
        modelBuilder.Entity<Game>().HasData(
            new Game { Id = 1, Name = "Blackjack",     Type = GameType.Blackjack, MinBet = 10, MaxBet = 500,  Description = "Classic card game" },
            new Game { Id = 2, Name = "Texas Hold'em", Type = GameType.Poker,     MinBet = 20, MaxBet = 1000, Description = "Most popular poker variant" },
            new Game { Id = 3, Name = "Roulette",      Type = GameType.Roulette,  MinBet = 5,  MaxBet = 300,  Description = "European single-zero roulette" },
            new Game { Id = 4, Name = "Lucky Slots",   Type = GameType.Slots,     MinBet = 1,  MaxBet = 50,   Description = "Progressive jackpot slots" }
        );

        // Casinos
        modelBuilder.Entity<Casino>().HasData(
            new Casino { Id = 1, Name = "Royal Vegas",    Address = "Ilica 12, Zagreb",        LicenseNumber = "HR-CAS-001", FoundedDate = new DateTime(2005, 4, 15) },
            new Casino { Id = 2, Name = "Golden Palace",  Address = "Vukovarska 55, Split",    LicenseNumber = "HR-CAS-002", FoundedDate = new DateTime(2010, 9, 1)  },
            new Casino { Id = 3, Name = "Diamond Club",   Address = "Korzo 3, Rijeka",         LicenseNumber = "HR-CAS-003", FoundedDate = new DateTime(2018, 2, 20) }
        );

        // Employees
        modelBuilder.Entity<Employee>().HasData(
            new Employee { Id = 1, FirstName = "Luka",     LastName = "Perić",     Position = "Dealer",   CasinoId = 1 },
            new Employee { Id = 2, FirstName = "Sara",     LastName = "Blažić",    Position = "Manager",  CasinoId = 1 },
            new Employee { Id = 3, FirstName = "Josip",    LastName = "Matić",     Position = "Security", CasinoId = 1 },
            new Employee { Id = 4, FirstName = "Maja",     LastName = "Šimić",     Position = "Dealer",   CasinoId = 2 },
            new Employee { Id = 5, FirstName = "Darko",    LastName = "Vukić",     Position = "Manager",  CasinoId = 2 },
            new Employee { Id = 6, FirstName = "Nikolina", LastName = "Čović",     Position = "Cashier",  CasinoId = 2 },
            new Employee { Id = 7, FirstName = "Bruno",    LastName = "Knežević",  Position = "Dealer",   CasinoId = 3 },
            new Employee { Id = 8, FirstName = "Ivana",    LastName = "Turić",     Position = "Manager",  CasinoId = 3 },
            new Employee { Id = 9, FirstName = "Ante",     LastName = "Grgić",     Position = "Security", CasinoId = 3 }
        );

        // Tables
        modelBuilder.Entity<Table>().HasData(
            new Table { Id = 1, TableNumber = 1, IsAvailable = true,  MinBet = 10, MaxBet = 500,  CasinoId = 1, GameId = 1 },
            new Table { Id = 2, TableNumber = 2, IsAvailable = false, MinBet = 20, MaxBet = 1000, CasinoId = 1, GameId = 2 },
            new Table { Id = 3, TableNumber = 3, IsAvailable = true,  MinBet = 5,  MaxBet = 300,  CasinoId = 1, GameId = 3 },
            new Table { Id = 4, TableNumber = 1, IsAvailable = true,  MinBet = 20, MaxBet = 1000, CasinoId = 2, GameId = 2 },
            new Table { Id = 5, TableNumber = 2, IsAvailable = true,  MinBet = 5,  MaxBet = 300,  CasinoId = 2, GameId = 3 },
            new Table { Id = 6, TableNumber = 3, IsAvailable = false, MinBet = 1,  MaxBet = 50,   CasinoId = 2, GameId = 4 },
            new Table { Id = 7, TableNumber = 1, IsAvailable = true,  MinBet = 10, MaxBet = 500,  CasinoId = 3, GameId = 1 },
            new Table { Id = 8, TableNumber = 2, IsAvailable = true,  MinBet = 1,  MaxBet = 50,   CasinoId = 3, GameId = 4 },
            new Table { Id = 9, TableNumber = 3, IsAvailable = false, MinBet = 20, MaxBet = 1000, CasinoId = 3, GameId = 2 }
        );

        // Players
        modelBuilder.Entity<Player>().HasData(
            new Player { Id = 1, FirstName = "Marko",    LastName = "Horvat", Email = "marko@mail.com",    DateOfBirth = new DateTime(1990, 5,  12), Balance = 1500 },
            new Player { Id = 2, FirstName = "Ana",      LastName = "Kovač",  Email = "ana@mail.com",      DateOfBirth = new DateTime(1995, 8,  23), Balance = 800  },
            new Player { Id = 3, FirstName = "Ivan",     LastName = "Babić",  Email = "ivan@mail.com",     DateOfBirth = new DateTime(1988, 3,   7), Balance = 3200 },
            new Player { Id = 4, FirstName = "Petra",    LastName = "Novak",  Email = "petra@mail.com",    DateOfBirth = new DateTime(1993, 11, 30), Balance = 600  },
            new Player { Id = 5, FirstName = "Tomislav", LastName = "Jurić",  Email = "tomislav@mail.com", DateOfBirth = new DateTime(1985, 6,  19), Balance = 5000 }
        );

        // Transactions
        modelBuilder.Entity<Transaction>().HasData(
            new Transaction { Id = 1, Amount = 500,  Type = TransactionType.Deposit,    CreatedAt = new DateTime(2024, 1, 10), PlayerId = 1 },
            new Transaction { Id = 2, Amount = 200,  Type = TransactionType.Bet,        CreatedAt = new DateTime(2024, 1, 10), PlayerId = 1 },
            new Transaction { Id = 3, Amount = 450,  Type = TransactionType.Win,        CreatedAt = new DateTime(2024, 1, 10), PlayerId = 1 },
            new Transaction { Id = 4, Amount = 300,  Type = TransactionType.Deposit,    CreatedAt = new DateTime(2024, 2, 5),  PlayerId = 2 },
            new Transaction { Id = 5, Amount = 150,  Type = TransactionType.Bet,        CreatedAt = new DateTime(2024, 2, 5),  PlayerId = 2 },
            new Transaction { Id = 6, Amount = 100,  Type = TransactionType.Withdrawal, CreatedAt = new DateTime(2024, 2, 6),  PlayerId = 2 },
            new Transaction { Id = 7, Amount = 1000, Type = TransactionType.Deposit,    CreatedAt = new DateTime(2024, 3, 1),  PlayerId = 3 },
            new Transaction { Id = 8, Amount = 500,  Type = TransactionType.Bet,        CreatedAt = new DateTime(2024, 3, 1),  PlayerId = 3 },
            new Transaction { Id = 9, Amount = 800,  Type = TransactionType.Win,        CreatedAt = new DateTime(2024, 3, 1),  PlayerId = 3 }
        );

        // Reservations
        modelBuilder.Entity<Reservation>().HasData(
            new Reservation { Id = 1, ReservedAt = new DateTime(2024, 4, 10, 20, 0, 0), PlayerId = 1, TableId = 1 },
            new Reservation { Id = 2, ReservedAt = new DateTime(2024, 4, 10, 21, 0, 0), PlayerId = 2, TableId = 4 },
            new Reservation { Id = 3, ReservedAt = new DateTime(2024, 4, 11, 18, 0, 0), PlayerId = 3, TableId = 7 },
            new Reservation { Id = 4, ReservedAt = new DateTime(2024, 4, 12, 19, 0, 0), PlayerId = 1, TableId = 5 },
            new Reservation { Id = 5, ReservedAt = new DateTime(2024, 4, 12, 22, 0, 0), PlayerId = 5, TableId = 2 }
        );
    }
}
