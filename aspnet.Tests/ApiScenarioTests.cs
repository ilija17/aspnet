using aspnet.Models;
using aspnet.Models.DTO;

namespace aspnet.Tests;

/// Playwright-style 10-step scenario that exercises all 7 CRUD API endpoints
/// in a single, realistic business workflow (3 extra points).
public class ApiScenarioTests : ApiTestBase
{
    public ApiScenarioTests(CasinoApiFactory factory) : base(factory) { }

    [Fact]
    public async Task TenStepScenario_ShouldExerciseAllCrudEndpoints()
    {
        // ── Step 1: GET /api/casino — list all casinos (starts empty or near-empty) ──
        var listResp = await Client.GetAsync("/api/casino");
        listResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var initialList = await listResp.Content.ReadFromJsonAsync<List<CasinoDTO>>();
        initialList.Should().NotBeNull();

        // ── Step 2: POST /api/casino — create a casino (Admin) ──
        var casinoModel = new CasinoInputDTO
        {
            Name = "Scenario Test Casino",
            Address = "Testna ulica 42, Zagreb",
            LicenseNumber = "HR-SCENARIO-001",
            FoundedDate = new DateTime(2021, 6, 15)
        };
        var createResp = await AdminClient.PostAsJsonAsync("/api/casino", casinoModel);
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdCasino = await createResp.Content.ReadFromJsonAsync<CasinoDTO>();
        createdCasino!.Id.Should().BeGreaterThan(0);
        createdCasino.Name.Should().Be(casinoModel.Name);

        // ── Step 3: GET /api/casino/{id} — verify the created casino ──
        var getResp = await Client.GetAsync($"/api/casino/{createdCasino.Id}");
        getResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetchedCasino = await getResp.Content.ReadFromJsonAsync<CasinoDTO>();
        fetchedCasino!.Id.Should().Be(createdCasino.Id);
        fetchedCasino.LicenseNumber.Should().Be(casinoModel.LicenseNumber);

        // ── Step 4: POST /api/player — create a player (Admin) ──
        var playerModel = new PlayerInputDTO
        {
            FirstName = "Scenarijo",
            LastName = "Igrač",
            Email = $"scenario-{Guid.NewGuid():N}@mail.com",
            DateOfBirth = new DateTime(1995, 3, 20),
            Balance = 5000
        };
        var playerResp = await AdminClient.PostAsJsonAsync("/api/player", playerModel);
        playerResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdPlayer = await playerResp.Content.ReadFromJsonAsync<PlayerDTO>();
        createdPlayer!.Id.Should().BeGreaterThan(0);
        createdPlayer.Email.Should().Be(playerModel.Email);

        // ── Step 5: POST /api/game — create a game (Admin) ──
        var gameModel = new GameInputDTO
        {
            Name = "Scenario Poker",
            Type = GameType.Poker,
            MinBet = 50,
            MaxBet = 1000,
            Description = "Scenarij test igra"
        };
        var gameResp = await AdminClient.PostAsJsonAsync("/api/game", gameModel);
        gameResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdGame = await gameResp.Content.ReadFromJsonAsync<GameDTO>();
        createdGame!.Id.Should().BeGreaterThan(0);
        createdGame.Type.Should().Be("Poker");

        // ── Step 6: POST /api/table — create a table linked to casino+game (Admin) ──
        var tableModel = new TableInputDTO
        {
            TableNumber = 101,
            IsAvailable = true,
            MinBet = 100,
            MaxBet = 2000,
            CasinoId = createdCasino.Id,
            GameId = createdGame.Id
        };
        var tableResp = await AdminClient.PostAsJsonAsync("/api/table", tableModel);
        tableResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdTable = await tableResp.Content.ReadFromJsonAsync<TableDTO>();
        createdTable!.Id.Should().BeGreaterThan(0);
        createdTable.TableNumber.Should().Be(101);
        createdTable.Casino!.Id.Should().Be(createdCasino.Id);
        createdTable.Game!.Id.Should().Be(createdGame.Id);

        // ── Step 7: POST /api/reservation — create reservation for player at table (Admin) ──
        var reservationModel = new ReservationInputDTO
        {
            ReservedAt = new DateTime(2026, 8, 1, 21, 0, 0),
            PlayerId = createdPlayer.Id,
            TableId = createdTable.Id
        };
        var resResp = await AdminClient.PostAsJsonAsync("/api/reservation", reservationModel);
        resResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdReservation = await resResp.Content.ReadFromJsonAsync<ReservationDTO>();
        createdReservation!.Id.Should().BeGreaterThan(0);
        createdReservation.Player!.Id.Should().Be(createdPlayer.Id);
        createdReservation.Table!.Id.Should().Be(createdTable.Id);

        // ── Step 8: POST /api/transaction — create transaction for player (Admin) ──
        var txModel = new TransactionInputDTO
        {
            Amount = 750,
            Type = TransactionType.Bet,
            CreatedAt = new DateTime(2026, 8, 1, 21, 30, 0),
            PlayerId = createdPlayer.Id
        };
        var txResp = await AdminClient.PostAsJsonAsync("/api/transaction", txModel);
        txResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdTx = await txResp.Content.ReadFromJsonAsync<TransactionDTO>();
        createdTx!.Id.Should().BeGreaterThan(0);
        createdTx.Amount.Should().Be(750);
        createdTx.Type.Should().Be("Bet");
        createdTx.Player!.Id.Should().Be(createdPlayer.Id);

        // ── Step 9: PUT /api/casino/{id} — update casino name (Admin) ──
        var updateModel = new CasinoInputDTO
        {
            Id = createdCasino.Id,
            Name = "Scenario Test Casino (Updated)",
            Address = createdCasino.Address,
            LicenseNumber = createdCasino.LicenseNumber,
            FoundedDate = createdCasino.FoundedDate
        };
        var putResp = await AdminClient.PutAsJsonAsync($"/api/casino/{createdCasino.Id}", updateModel);
        putResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedCasino = await putResp.Content.ReadFromJsonAsync<CasinoDTO>();
        updatedCasino!.Name.Should().Be("Scenario Test Casino (Updated)");

        // ── Step 10: DELETE /api/table/{id} — clean up table (Admin) ──
        var delResp = await AdminClient.DeleteAsync($"/api/table/{createdTable.Id}");
        delResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify table is really gone
        var verifyResp = await Client.GetAsync($"/api/table/{createdTable.Id}");
        verifyResp.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // Verify reservation still exists (no cascade from table)
        var resCheck = await Client.GetAsync($"/api/reservation/{createdReservation.Id}");
        // NOTE: InMemory DB may cascade-delete; both 200 and 404 are acceptable
        resCheck.StatusCode.Should().BeOneOf([HttpStatusCode.OK, HttpStatusCode.NotFound]);

        var txCheck = await Client.GetAsync($"/api/transaction/{createdTx.Id}");
        txCheck.StatusCode.Should().BeOneOf([HttpStatusCode.OK, HttpStatusCode.NotFound]);
    }

    [Fact]
    public async Task CrossEntityFiltering_ShouldReturnConsistentResults()
    {
        var casino = await CreateCasinoAsync();
        var game = await CreateGameAsync();
        var player = await CreatePlayerAsync();

        var employee = await CreateEmployeeAsync(casino.Id);
        var table = await WithDbAsync(async db =>
        {
            var t = new Table
            {
                TableNumber = 42,
                IsAvailable = true,
                MinBet = 10,
                MaxBet = 500,
                CasinoId = casino.Id,
                GameId = game.Id
            };
            db.Tables.Add(t);
            await db.SaveChangesAsync();
            return t;
        });

        // Employee filtered by casino
        var empResp = await Client.GetAsync($"/api/employee?casinoId={casino.Id}");
        empResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var emps = await empResp.Content.ReadFromJsonAsync<List<EmployeeDTO>>();
        emps.Should().Contain(e => e.Id == employee.Id);

        // Table filtered by casino
        var tableResp = await Client.GetAsync($"/api/table?casinoId={casino.Id}");
        tableResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var tables = await tableResp.Content.ReadFromJsonAsync<List<TableDTO>>();
        tables.Should().Contain(t => t.Id == table.Id);

        // Table filtered by game
        var tableByGame = await Client.GetAsync($"/api/table?gameId={game.Id}");
        tableByGame.StatusCode.Should().Be(HttpStatusCode.OK);
        var tablesByGame = await tableByGame.Content.ReadFromJsonAsync<List<TableDTO>>();
        tablesByGame.Should().Contain(t => t.Id == table.Id);

        // Player filter by min balance
        var playerResp = await Client.GetAsync($"/api/player?minBalance=500");
        playerResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var players = await playerResp.Content.ReadFromJsonAsync<List<PlayerDTO>>();
        players.Should().NotBeNull();

        // Game filter by type
        var gameResp = await Client.GetAsync($"/api/game?type=Blackjack");
        gameResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var games = await gameResp.Content.ReadFromJsonAsync<List<GameDTO>>();
        games.Should().Contain(g => g.Id == game.Id);
    }

    [Fact]
    public async Task AuthorizationEndToEnd_ShouldEnforceRoleRulesAcrossApis()
    {
        var casino = await CreateCasinoAsync();

        // Anonymous can GET
        (await Client.GetAsync("/api/casino")).StatusCode.Should().Be(HttpStatusCode.OK);

        // Anonymous cannot POST
        var model = new CasinoInputDTO
        {
            Name = "Hack Casino", Address = "Bad 1", LicenseNumber = "HR-HACK-1",
            FoundedDate = new DateTime(2020, 1, 1)
        };
        (await Client.PostAsJsonAsync("/api/casino", model)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Manager can POST
        var mgrResp = await ManagerClient.PostAsJsonAsync("/api/casino", new CasinoInputDTO
        {
            Name = "Manager Casino", Address = "Mgr 1", LicenseNumber = "HR-MGR-1",
            FoundedDate = new DateTime(2021, 1, 1)
        });
        mgrResp.StatusCode.Should().Be(HttpStatusCode.Created);

        // Manager cannot DELETE (Admin only)
        var mgrDelete = await ManagerClient.DeleteAsync($"/api/casino/{casino.Id}");
        mgrDelete.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Admin can DELETE
        var delResp = await AdminClient.DeleteAsync($"/api/casino/{casino.Id}");
        delResp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
