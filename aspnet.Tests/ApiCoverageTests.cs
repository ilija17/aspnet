using System.Net.Http.Json;
using System.Text.Json;
using aspnet.Models;
using aspnet.Models.DTO;

namespace aspnet.Tests;

/// Covers remaining API test gaps: Waitlist, advanced filters, Manager role
/// authorization on POST/PUT, and additional edge cases.
public class ApiCoverageTests : ApiTestBase
{
    public ApiCoverageTests(CasinoApiFactory factory) : base(factory) { }

    // ═══════════════════════════════════════════════════════════════
    //  WAITLIST
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task Waitlist_ShouldAcceptValidEmail_AndReturnPosition()
    {
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("email", $"waitlist-{Guid.NewGuid():N}@test.com")
        });

        var resp = await Client.PostAsync("/Home/Waitlist", content);

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("position").GetInt32().Should().BeGreaterThan(12000);
        json.GetProperty("alreadyJoined").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task Waitlist_DuplicateEmail_ShouldReturnAlreadyJoined()
    {
        var email = $"dup-{Guid.NewGuid():N}@test.com";
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("email", email)
        });

        var first = await Client.PostAsync("/Home/Waitlist", content);
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        var second = await Client.PostAsync("/Home/Waitlist", content);
        second.StatusCode.Should().Be(HttpStatusCode.OK);

        // The duplicate response is JSON; verify it has alreadyJoined=true
        var body = await second.Content.ReadAsStringAsync();
        body.Should().Contain("alreadyJoined").And.Contain("true");
    }

    [Fact]
    public async Task Waitlist_ShouldReturn400_WhenEmailInvalid()
    {
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("email", "not-an-email")
        });

        var resp = await Client.PostAsync("/Home/Waitlist", content);

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Waitlist_ShouldReturn400_WhenEmailEmpty()
    {
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("email", "")
        });

        var resp = await Client.PostAsync("/Home/Waitlist", content);

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ═══════════════════════════════════════════════════════════════
    //  ADVANCED FILTERS (not covered by existing tests)
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task Casino_GetAll_WithFoundedAfter_ShouldFilterByDate()
    {
        var oldCasino = await CreateCasinoAsync();
        var newCasino = await WithDbAsync(async db =>
        {
            var c = new Casino
            {
                Name = $"New Casino {Guid.NewGuid():N}",
                Address = "New address",
                LicenseNumber = "NEW-999",
                FoundedDate = new DateTime(2025, 1, 1)
            };
            db.Casinos.Add(c);
            await db.SaveChangesAsync();
            return c;
        });

        var resp = await Client.GetAsync("/api/casino?foundedAfter=2024-06-01");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var dtos = await resp.Content.ReadFromJsonAsync<List<CasinoDTO>>();
        dtos.Should().Contain(c => c.Id == newCasino.Id);
        dtos.Should().NotContain(c => c.Id == oldCasino.Id);
    }

    [Fact]
    public async Task Player_GetAll_WithMinBalance_ShouldFilterByBalance()
    {
        var lowPlayer = await WithDbAsync(async db =>
        {
            var p = new Player
            {
                FirstName = "Low", LastName = "Balance",
                Email = $"low-{Guid.NewGuid():N}@mail.com",
                DateOfBirth = new DateTime(1990, 1, 1), Balance = 10
            };
            db.Players.Add(p);
            await db.SaveChangesAsync();
            return p;
        });
        var highPlayer = await WithDbAsync(async db =>
        {
            var p = new Player
            {
                FirstName = "High", LastName = "Balance",
                Email = $"high-{Guid.NewGuid():N}@mail.com",
                DateOfBirth = new DateTime(1990, 1, 1), Balance = 99999
            };
            db.Players.Add(p);
            await db.SaveChangesAsync();
            return p;
        });

        var resp = await Client.GetAsync("/api/player?minBalance=50000");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var dtos = await resp.Content.ReadFromJsonAsync<List<PlayerDTO>>();
        dtos.Should().Contain(p => p.Id == highPlayer.Id);
        dtos.Should().NotContain(p => p.Id == lowPlayer.Id);
    }

    [Fact]
    public async Task Game_GetAll_WithTypeFilter_ShouldFilterByGameType()
    {
        var game = await CreateGameAsync(); // defaults to Blackjack

        await WithDbAsync<bool>(async db =>
        {
            db.Games.Add(new Game
            {
                Name = $"Slots Game {Guid.NewGuid():N}",
                Type = GameType.Slots, MinBet = 1, MaxBet = 50,
                Description = "Slots game for filter test"
            });
            await db.SaveChangesAsync();
            return true;
        });

        var resp = await Client.GetAsync("/api/game?type=Blackjack");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var dtos = await resp.Content.ReadFromJsonAsync<List<GameDTO>>();
        dtos.Should().Contain(g => g.Id == game.Id);
        dtos.Should().OnlyContain(g => g.Type == "Blackjack");
    }

    [Fact]
    public async Task Table_GetAll_WithAvailableFilter_ShouldShowAvailableTables()
    {
        var table = await CreateTableAsync(); // IsAvailable = true by default

        var resp = await Client.GetAsync($"/api/table?available=true");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var dtos = await resp.Content.ReadFromJsonAsync<List<TableDTO>>();
        dtos.Should().Contain(t => t.Id == table.Id && t.IsAvailable);
    }

    [Fact]
    public async Task Table_GetAll_WithGameFilter_ShouldFilterByGameId()
    {
        var game = await CreateGameAsync();
        var table = await WithDbAsync(async db =>
        {
            var casino = new Casino
            {
                Name = $"CT {Guid.NewGuid():N}", Address = "Addr",
                LicenseNumber = "LIC", FoundedDate = new DateTime(2020, 1, 1)
            };
            db.Casinos.Add(casino);
            await db.SaveChangesAsync();

            var t = new Table
            {
                TableNumber = 77, IsAvailable = true, MinBet = 10, MaxBet = 500,
                CasinoId = casino.Id, GameId = game.Id
            };
            db.Tables.Add(t);
            await db.SaveChangesAsync();
            return t;
        });

        var resp = await Client.GetAsync($"/api/table?gameId={game.Id}");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var dtos = await resp.Content.ReadFromJsonAsync<List<TableDTO>>();
        dtos.Should().Contain(t => t.Id == table.Id);
    }

    [Fact]
    public async Task Reservation_GetAll_WithFromDate_ShouldFilterByDate()
    {
        var oldRes = await CreateReservationAsync(); // ReservedAt = 2024-06-01

        var futureRes = await WithDbAsync(async db =>
        {
            var player = new Player
            {
                FirstName = "F", LastName = "P",
                Email = $"fp-{Guid.NewGuid():N}@mail.com",
                DateOfBirth = new DateTime(1990, 1, 1), Balance = 100
            };
            db.Players.Add(player);
            await db.SaveChangesAsync();

            var r = new Reservation
            {
                ReservedAt = new DateTime(2026, 1, 1),
                PlayerId = player.Id, TableId = oldRes.TableId
            };
            db.Reservations.Add(r);
            await db.SaveChangesAsync();
            return r;
        });

        var resp = await Client.GetAsync("/api/reservation?from=2025-01-01");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var dtos = await resp.Content.ReadFromJsonAsync<List<ReservationDTO>>();
        dtos.Should().Contain(r => r.Id == futureRes.Id);
        dtos.Should().NotContain(r => r.Id == oldRes.Id);
    }

    [Fact]
    public async Task Transaction_GetAll_WithTypeFilter_ShouldFilterByType()
    {
        var player = await CreatePlayerAsync();
        var deposit = await WithDbAsync(async db =>
        {
            var t = new Transaction
            {
                Amount = 100, Type = TransactionType.Deposit,
                CreatedAt = new DateTime(2024, 1, 1), PlayerId = player.Id
            };
            db.Transactions.Add(t);
            await db.SaveChangesAsync();
            return t;
        });
        var bet = await WithDbAsync(async db =>
        {
            var t = new Transaction
            {
                Amount = 100, Type = TransactionType.Bet,
                CreatedAt = new DateTime(2024, 2, 1), PlayerId = player.Id
            };
            db.Transactions.Add(t);
            await db.SaveChangesAsync();
            return t;
        });

        var resp = await Client.GetAsync("/api/transaction?type=Deposit");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var dtos = await resp.Content.ReadFromJsonAsync<List<TransactionDTO>>();
        dtos.Should().Contain(t => t.Id == deposit.Id && t.Type == "Deposit");
        dtos.Should().NotContain(t => t.Id == bet.Id);
    }

    [Fact]
    public async Task Employee_GetAll_WithSearchQuery_ShouldFilterByText()
    {
        var emp = await CreateEmployeeAsync();
        var query = emp.LastName[..Math.Min(5, emp.LastName.Length)];

        var resp = await Client.GetAsync($"/api/employee?q={query}");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var dtos = await resp.Content.ReadFromJsonAsync<List<EmployeeDTO>>();
        dtos.Should().Contain(e => e.Id == emp.Id);
    }

    // ═══════════════════════════════════════════════════════════════
    //  MANAGER ROLE TESTS (Manager can POST/PUT, but NOT DELETE)
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task Manager_CanPostPlayer()
    {
        var model = new PlayerInputDTO
        {
            FirstName = "Mgr", LastName = "Created",
            Email = $"mgr-{Guid.NewGuid():N}@mail.com",
            DateOfBirth = new DateTime(1990, 1, 1), Balance = 100
        };
        var resp = await ManagerClient.PostAsJsonAsync("/api/player", model);
        resp.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Manager_CanPostGame()
    {
        var model = new GameInputDTO
        {
            Name = "Manager Game",
            Type = GameType.Roulette, MinBet = 10, MaxBet = 500
        };
        var resp = await ManagerClient.PostAsJsonAsync("/api/game", model);
        resp.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Manager_CanPutEmployee()
    {
        var emp = await CreateEmployeeAsync();
        var model = new EmployeeInputDTO
        {
            Id = emp.Id, FirstName = "ManagerRenamed",
            LastName = emp.LastName, Position = "Supervisor",
            CasinoId = emp.CasinoId
        };
        var resp = await ManagerClient.PutAsJsonAsync($"/api/employee/{emp.Id}", model);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Manager_CanPutTransaction()
    {
        var tx = await CreateTransactionAsync();
        var model = new TransactionInputDTO
        {
            Id = tx.Id, Amount = 500, Type = TransactionType.Win,
            CreatedAt = tx.CreatedAt, PlayerId = tx.PlayerId
        };
        var resp = await ManagerClient.PutAsJsonAsync($"/api/transaction/{tx.Id}", model);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Manager_CannotDeleteCasino()
    {
        var casino = await CreateCasinoAsync();
        var resp = await ManagerClient.DeleteAsync($"/api/casino/{casino.Id}");
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Manager_CannotDeletePlayer()
    {
        var player = await CreatePlayerAsync();
        var resp = await ManagerClient.DeleteAsync($"/api/player/{player.Id}");
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ═══════════════════════════════════════════════════════════════
    //  CHAT API
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task Chat_ShouldReturn503_WhenApiKeyNotConfigured()
    {
        var model = new { messages = new[] { new { role = "user", content = "Hello" } } };

        var resp = await Client.PostAsJsonAsync("/api/chat", model);

        resp.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task Chat_ShouldReturn400_WhenNoMessages()
    {
        var resp = await Client.PostAsJsonAsync("/api/chat", new { messages = Array.Empty<object>() });
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ═══════════════════════════════════════════════════════════════
    //  HTTP METHOD COVERAGE (ALL combinations)
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task AllGetEndpoints_ShouldReturnOkOr404()
    {
        var eps = new[]
        {
            "/api/casino", "/api/player", "/api/game", "/api/table",
            "/api/employee", "/api/reservation", "/api/transaction"
        };

        foreach (var ep in eps)
        {
            var resp = await Client.GetAsync(ep);
            resp.StatusCode.Should().Be(HttpStatusCode.OK,
                $"GET {ep} should return 200 OK");
        }
    }

    [Fact]
    public async Task ManagerPost_CasinoTableReservation_ShouldCreate()
    {
        var casinoResp = await ManagerClient.PostAsJsonAsync("/api/casino", new CasinoInputDTO
        {
            Name = "Mgr Casino Complex",
            Address = "Mgr street 1",
            LicenseNumber = $"HR-MGR-{Guid.NewGuid():N}"[..20],
            FoundedDate = new DateTime(2023, 1, 1)
        });
        casinoResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var casino = await casinoResp.Content.ReadFromJsonAsync<CasinoDTO>();

        var gameResp = await ManagerClient.PostAsJsonAsync("/api/game", new GameInputDTO
        {
            Name = "Mgr Game",
            Type = GameType.Blackjack, MinBet = 5, MaxBet = 100
        });
        gameResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var game = await gameResp.Content.ReadFromJsonAsync<GameDTO>();

        var tableResp = await ManagerClient.PostAsJsonAsync("/api/table", new TableInputDTO
        {
            TableNumber = 1, IsAvailable = true, MinBet = 10, MaxBet = 200,
            CasinoId = casino!.Id, GameId = game!.Id
        });
        tableResp.StatusCode.Should().Be(HttpStatusCode.Created);

        var playerResp = await ManagerClient.PostAsJsonAsync("/api/player", new PlayerInputDTO
        {
            FirstName = "Mgr", LastName = "Player",
            Email = $"mgrp-{Guid.NewGuid():N}@mail.com",
            DateOfBirth = new DateTime(1990, 1, 1), Balance = 100
        });
        playerResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var player = await playerResp.Content.ReadFromJsonAsync<PlayerDTO>();

        var resResp = await ManagerClient.PostAsJsonAsync("/api/reservation", new ReservationInputDTO
        {
            ReservedAt = new DateTime(2026, 12, 25),
            PlayerId = player!.Id, TableId = 1
        });
        resResp.StatusCode.Should().BeOneOf([HttpStatusCode.Created, HttpStatusCode.OK]);
    }

    [Fact]
    public async Task Validation_MalformedJson_ShouldReturn400()
    {
        var content = new StringContent("{ not valid json ", System.Text.Encoding.UTF8, "application/json");
        var resp = await AdminClient.PostAsync("/api/casino", content);
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_ShouldReturn415_WhenContentTypeWrong()
    {
        var content = new StringContent("name=test", System.Text.Encoding.UTF8, "text/plain");
        var resp = await AdminClient.PostAsync("/api/casino", content);
        resp.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType);
    }
}
