using aspnet.Data;
using aspnet.Models;
using Microsoft.Extensions.DependencyInjection;

namespace aspnet.Tests;

public abstract class ApiTestBase : IClassFixture<CasinoApiFactory>
{
    protected readonly CasinoApiFactory Factory;

    /// Klijent bez autentikacije
    protected readonly HttpClient Client;

    /// Klijent s rolom Admin
    protected readonly HttpClient AdminClient;

    /// Klijent s rolom Manager
    protected readonly HttpClient ManagerClient;

    protected ApiTestBase(CasinoApiFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();

        AdminClient = factory.CreateClient();
        AdminClient.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, "Admin");

        ManagerClient = factory.CreateClient();
        ManagerClient.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, "Manager");
    }

    protected async Task<T> WithDbAsync<T>(Func<CasinoDbContext, Task<T>> action)
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CasinoDbContext>();
        return await action(dbContext);
    }

    // ── Pomoćne metode za pripremu podataka ──────────────────────────────────

    protected Task<Casino> CreateCasinoAsync() => WithDbAsync(async db =>
    {
        var casino = new Casino
        {
            Name = $"Test Casino {Guid.NewGuid():N}",
            Address = "Testna ulica 1, Zagreb",
            LicenseNumber = $"HR-TST-{Random.Shared.Next(1000, 9999)}",
            FoundedDate = new DateTime(2015, 1, 1)
        };
        db.Casinos.Add(casino);
        await db.SaveChangesAsync();
        return casino;
    });

    protected Task<Game> CreateGameAsync() => WithDbAsync(async db =>
    {
        var game = new Game
        {
            Name = $"Test Game {Guid.NewGuid():N}",
            Type = GameType.Blackjack,
            MinBet = 10,
            MaxBet = 500,
            Description = "Testna igra"
        };
        db.Games.Add(game);
        await db.SaveChangesAsync();
        return game;
    });

    protected Task<Player> CreatePlayerAsync() => WithDbAsync(async db =>
    {
        var player = new Player
        {
            FirstName = "Test",
            LastName = $"Player {Guid.NewGuid():N}",
            Email = $"test-{Guid.NewGuid():N}@mail.com",
            DateOfBirth = new DateTime(1990, 1, 1),
            Balance = 1000
        };
        db.Players.Add(player);
        await db.SaveChangesAsync();
        return player;
    });

    protected async Task<Employee> CreateEmployeeAsync(int? casinoId = null)
    {
        casinoId ??= (await CreateCasinoAsync()).Id;
        return await WithDbAsync(async db =>
        {
            var employee = new Employee
            {
                FirstName = "Test",
                LastName = $"Employee {Guid.NewGuid():N}",
                Position = "Dealer",
                CasinoId = casinoId.Value
            };
            db.Employees.Add(employee);
            await db.SaveChangesAsync();
            return employee;
        });
    }

    protected async Task<Table> CreateTableAsync(int? casinoId = null, int? gameId = null)
    {
        casinoId ??= (await CreateCasinoAsync()).Id;
        gameId ??= (await CreateGameAsync()).Id;
        return await WithDbAsync(async db =>
        {
            var table = new Table
            {
                TableNumber = Random.Shared.Next(1, 1000),
                IsAvailable = true,
                MinBet = 10,
                MaxBet = 500,
                CasinoId = casinoId.Value,
                GameId = gameId.Value
            };
            db.Tables.Add(table);
            await db.SaveChangesAsync();
            return table;
        });
    }

    protected async Task<Transaction> CreateTransactionAsync(int? playerId = null)
    {
        playerId ??= (await CreatePlayerAsync()).Id;
        return await WithDbAsync(async db =>
        {
            var transaction = new Transaction
            {
                Amount = 100,
                Type = TransactionType.Deposit,
                CreatedAt = new DateTime(2024, 5, 1),
                PlayerId = playerId.Value
            };
            db.Transactions.Add(transaction);
            await db.SaveChangesAsync();
            return transaction;
        });
    }

    protected async Task<Reservation> CreateReservationAsync(int? playerId = null, int? tableId = null)
    {
        playerId ??= (await CreatePlayerAsync()).Id;
        tableId ??= (await CreateTableAsync()).Id;
        return await WithDbAsync(async db =>
        {
            var reservation = new Reservation
            {
                ReservedAt = new DateTime(2024, 6, 1, 20, 0, 0),
                PlayerId = playerId.Value,
                TableId = tableId.Value
            };
            db.Reservations.Add(reservation);
            await db.SaveChangesAsync();
            return reservation;
        });
    }
}
