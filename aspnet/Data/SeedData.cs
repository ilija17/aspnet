using aspnet.Models;

namespace aspnet.Data;

// Holds the in-memory seed data that is built once at startup.
public record CasinoSeedData(
    List<Casino>     Casinos,
    List<Player>     Players,
    List<Reservation> Reservations
);

// Builds all in-memory objects (games, casinos, players, transactions,
// reservations) and wires up their navigation properties.
public static class SeedData
{
    public static CasinoSeedData Create()
    {
        // ── Games ─────────────────────────────────────────────────────────────
        var blackjack = new Game { Id = 1, Name = "Blackjack",    Type = GameType.Blackjack, MinBet = 10, MaxBet = 500,  Description = "Classic card game" };
        var poker     = new Game { Id = 2, Name = "Texas Hold'em",Type = GameType.Poker,     MinBet = 20, MaxBet = 1000, Description = "Most popular poker variant" };
        var roulette  = new Game { Id = 3, Name = "Roulette",     Type = GameType.Roulette,  MinBet = 5,  MaxBet = 300,  Description = "European single-zero roulette" };
        var slots     = new Game { Id = 4, Name = "Lucky Slots",  Type = GameType.Slots,     MinBet = 1,  MaxBet = 50,   Description = "Progressive jackpot slots" };

        // ── Players ───────────────────────────────────────────────────────────
        var player1 = new Player { Id = 1, FirstName = "Marko",    LastName = "Horvat",  Email = "marko@mail.com",    DateOfBirth = new DateTime(1990, 5,  12), Balance = 1500 };
        var player2 = new Player { Id = 2, FirstName = "Ana",      LastName = "Kovač",   Email = "ana@mail.com",      DateOfBirth = new DateTime(1995, 8,  23), Balance = 800  };
        var player3 = new Player { Id = 3, FirstName = "Ivan",     LastName = "Babić",   Email = "ivan@mail.com",     DateOfBirth = new DateTime(1988, 3,   7), Balance = 3200 };
        var player4 = new Player { Id = 4, FirstName = "Petra",    LastName = "Novak",   Email = "petra@mail.com",    DateOfBirth = new DateTime(1993, 11, 30), Balance = 600  };
        var player5 = new Player { Id = 5, FirstName = "Tomislav", LastName = "Jurić",   Email = "tomislav@mail.com", DateOfBirth = new DateTime(1985, 6,  19), Balance = 5000 };

        // ── Casinos (each with 3 tables and 3 employees) ──────────────────────
        var casino1 = new Casino
        {
            Id = 1, Name = "Royal Vegas", Address = "Ilica 12, Zagreb",
            LicenseNumber = "HR-CAS-001", FoundedDate = new DateTime(2005, 4, 15),
            Employees = new List<Employee>
            {
                new() { Id = 1, FirstName = "Luka",  LastName = "Perić",  Position = "Dealer",   CasinoId = 1 },
                new() { Id = 2, FirstName = "Sara",  LastName = "Blažić", Position = "Manager",  CasinoId = 1 },
                new() { Id = 3, FirstName = "Josip", LastName = "Matić",  Position = "Security", CasinoId = 1 },
            },
            Tables = new List<Table>
            {
                new() { Id = 1, TableNumber = 1, IsAvailable = true,  MinBet = 10, MaxBet = 500,  CasinoId = 1, Game = blackjack, GameId = 1 },
                new() { Id = 2, TableNumber = 2, IsAvailable = false, MinBet = 20, MaxBet = 1000, CasinoId = 1, Game = poker,     GameId = 2 },
                new() { Id = 3, TableNumber = 3, IsAvailable = true,  MinBet = 5,  MaxBet = 300,  CasinoId = 1, Game = roulette,  GameId = 3 },
            }
        };

        var casino2 = new Casino
        {
            Id = 2, Name = "Golden Palace", Address = "Vukovarska 55, Split",
            LicenseNumber = "HR-CAS-002", FoundedDate = new DateTime(2010, 9, 1),
            Employees = new List<Employee>
            {
                new() { Id = 4, FirstName = "Maja",     LastName = "Šimić", Position = "Dealer",  CasinoId = 2 },
                new() { Id = 5, FirstName = "Darko",    LastName = "Vukić", Position = "Manager", CasinoId = 2 },
                new() { Id = 6, FirstName = "Nikolina", LastName = "Čović", Position = "Cashier", CasinoId = 2 },
            },
            Tables = new List<Table>
            {
                new() { Id = 4, TableNumber = 1, IsAvailable = true,  MinBet = 20, MaxBet = 1000, CasinoId = 2, Game = poker,    GameId = 2 },
                new() { Id = 5, TableNumber = 2, IsAvailable = true,  MinBet = 5,  MaxBet = 300,  CasinoId = 2, Game = roulette, GameId = 3 },
                new() { Id = 6, TableNumber = 3, IsAvailable = false, MinBet = 1,  MaxBet = 50,   CasinoId = 2, Game = slots,    GameId = 4 },
            }
        };

        var casino3 = new Casino
        {
            Id = 3, Name = "Diamond Club", Address = "Korzo 3, Rijeka",
            LicenseNumber = "HR-CAS-003", FoundedDate = new DateTime(2018, 2, 20),
            Employees = new List<Employee>
            {
                new() { Id = 7, FirstName = "Bruno", LastName = "Knežević", Position = "Dealer",   CasinoId = 3 },
                new() { Id = 8, FirstName = "Ivana", LastName = "Turić",    Position = "Manager",  CasinoId = 3 },
                new() { Id = 9, FirstName = "Ante",  LastName = "Grgić",    Position = "Security", CasinoId = 3 },
            },
            Tables = new List<Table>
            {
                new() { Id = 7, TableNumber = 1, IsAvailable = true,  MinBet = 10, MaxBet = 500,  CasinoId = 3, Game = blackjack, GameId = 1 },
                new() { Id = 8, TableNumber = 2, IsAvailable = true,  MinBet = 1,  MaxBet = 50,   CasinoId = 3, Game = slots,     GameId = 4 },
                new() { Id = 9, TableNumber = 3, IsAvailable = false, MinBet = 20, MaxBet = 1000, CasinoId = 3, Game = poker,     GameId = 2 },
            }
        };

        foreach (var t in casino1.Tables) t.Casino = casino1;
        foreach (var t in casino2.Tables) t.Casino = casino2;
        foreach (var t in casino3.Tables) t.Casino = casino3;

        // ── Transactions (attached to players) ────────────────────────────────
        player1.Transactions.Add(new Transaction { Id = 1, Amount = 500,  Type = TransactionType.Deposit,    CreatedAt = new DateTime(2024, 1, 10), PlayerId = 1, Player = player1 });
        player1.Transactions.Add(new Transaction { Id = 2, Amount = 200,  Type = TransactionType.Bet,        CreatedAt = new DateTime(2024, 1, 10), PlayerId = 1, Player = player1 });
        player1.Transactions.Add(new Transaction { Id = 3, Amount = 450,  Type = TransactionType.Win,        CreatedAt = new DateTime(2024, 1, 10), PlayerId = 1, Player = player1 });

        player2.Transactions.Add(new Transaction { Id = 4, Amount = 300,  Type = TransactionType.Deposit,    CreatedAt = new DateTime(2024, 2, 5),  PlayerId = 2, Player = player2 });
        player2.Transactions.Add(new Transaction { Id = 5, Amount = 150,  Type = TransactionType.Bet,        CreatedAt = new DateTime(2024, 2, 5),  PlayerId = 2, Player = player2 });
        player2.Transactions.Add(new Transaction { Id = 6, Amount = 100,  Type = TransactionType.Withdrawal, CreatedAt = new DateTime(2024, 2, 6),  PlayerId = 2, Player = player2 });

        player3.Transactions.Add(new Transaction { Id = 7, Amount = 1000, Type = TransactionType.Deposit,    CreatedAt = new DateTime(2024, 3, 1),  PlayerId = 3, Player = player3 });
        player3.Transactions.Add(new Transaction { Id = 8, Amount = 500,  Type = TransactionType.Bet,        CreatedAt = new DateTime(2024, 3, 1),  PlayerId = 3, Player = player3 });
        player3.Transactions.Add(new Transaction { Id = 9, Amount = 800,  Type = TransactionType.Win,        CreatedAt = new DateTime(2024, 3, 1),  PlayerId = 3, Player = player3 });

        // ── Reservations – N-N between Player and Table ───────────────────────
        var c1Tables = casino1.Tables.ToList();
        var c2Tables = casino2.Tables.ToList();
        var c3Tables = casino3.Tables.ToList();

        var reservations = new List<Reservation>
        {
            new() { Id = 1, ReservedAt = new DateTime(2024, 4, 10, 20, 0, 0), PlayerId = 1, Player = player1, TableId = 1, Table = c1Tables[0] },
            new() { Id = 2, ReservedAt = new DateTime(2024, 4, 10, 21, 0, 0), PlayerId = 2, Player = player2, TableId = 4, Table = c2Tables[0] },
            new() { Id = 3, ReservedAt = new DateTime(2024, 4, 11, 18, 0, 0), PlayerId = 3, Player = player3, TableId = 7, Table = c3Tables[0] },
            new() { Id = 4, ReservedAt = new DateTime(2024, 4, 12, 19, 0, 0), PlayerId = 1, Player = player1, TableId = 5, Table = c2Tables[1] },
            new() { Id = 5, ReservedAt = new DateTime(2024, 4, 12, 22, 0, 0), PlayerId = 5, Player = player5, TableId = 2, Table = c1Tables[1] },
        };

        // Wire reservations back onto players (the N side of N-N)
        player1.Reservations.Add(reservations[0]);
        player1.Reservations.Add(reservations[3]);
        player2.Reservations.Add(reservations[1]);
        player3.Reservations.Add(reservations[2]);
        player5.Reservations.Add(reservations[4]);

        return new CasinoSeedData(
            Casinos:      new List<Casino> { casino1, casino2, casino3 },
            Players:      new List<Player> { player1, player2, player3, player4, player5 },
            Reservations: reservations
        );
    }
}
