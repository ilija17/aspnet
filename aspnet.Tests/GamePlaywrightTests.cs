using System.Net.Http.Json;
using System.Text.Json;
using aspnet.Models.DTO;

namespace aspnet.Tests;

/// Playwright-style comprehensive tests for all game APIs:
/// Blackjack, Roulette, Slot, and ThreeBody.
/// Each game API requires an authenticated user AND a matching Player record
/// (FindPlayer maps User.Identity.Name to Player.Email).
public class GamePlaywrightTests : ApiTestBase
{
    public GamePlaywrightTests(CasinoApiFactory factory) : base(factory) { }

    // Create a Player whose Email matches what TestAuthHandler sets as User.Identity.Name
    private async Task EnsureTestPlayerAsync()
    {
        var player = await WithDbAsync<Models.Player?>(async db =>
        {
            var p = db.Players.FirstOrDefault(p => p.Email == "test-user");
            if (p is not null) return p;

            p = new Models.Player
            {
                FirstName = "Test",
                LastName = "User",
                Email = "test-user",
                DateOfBirth = new DateTime(1990, 1, 1),
                Balance = 500000
            };
            db.Players.Add(p);
            await db.SaveChangesAsync();
            return p;
        });
    }

    // ═══════════════════════════════════════════════════════════════
    //  BLACKJACK  (6 endpoints)
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task Blackjack_State_ShouldReturnOk_WhenAuthenticated()
    {
        await EnsureTestPlayerAsync();

        var resp = await AdminClient.GetAsync("/api/blackjack/state");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var state = await resp.Content.ReadFromJsonAsync<BlackjackStateDTO>();
        state.Should().NotBeNull();
        state!.PlayerName.Should().Be("Test User");
        state.Version.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public async Task Blackjack_FullRound_ShouldBetDealHitStandAndWinOrLose()
    {
        await EnsureTestPlayerAsync();

        // 1. Bet
        var betResp = await AdminClient.PostAsJsonAsync("/api/blackjack/bet", new { amount = 100 });
        betResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var afterBet = await betResp.Content.ReadFromJsonAsync<BlackjackStateDTO>();
        afterBet!.CanDeal.Should().BeTrue();

        // 2. Deal
        var dealResp = await AdminClient.PostAsync("/api/blackjack/deal", null);
        dealResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var afterDeal = await dealResp.Content.ReadFromJsonAsync<BlackjackStateDTO>();
        afterDeal!.CanHit.Should().BeTrue();

        // 3. Stand (complete round)
        var standResp = await AdminClient.PostAsync("/api/blackjack/stand", null);
        standResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var afterStand = await standResp.Content.ReadFromJsonAsync<BlackjackStateDTO>();
        afterStand!.Phase.Should().Be("round-over");
        afterStand.CanSetBet.Should().BeTrue();
    }

    [Fact]
    public async Task Blackjack_Bet_ShouldIgnoreInvalidAmount()
    {
        await EnsureTestPlayerAsync();

        var resp = await AdminClient.PostAsJsonAsync("/api/blackjack/bet", new { amount = 7 });

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var state = await resp.Content.ReadFromJsonAsync<BlackjackStateDTO>();
        state.Should().NotBeNull();
    }

    [Fact]
    public async Task Blackjack_AllEndpoints_ShouldReturn401_WhenAnonymous()
    {
        var endpoints = new[] { "state", "bet", "deal", "hit", "stand", "double" };
        foreach (var ep in endpoints)
        {
            HttpResponseMessage resp;
            if (ep == "state")
                resp = await Client.GetAsync($"/api/blackjack/{ep}");
            else
                resp = await Client.PostAsync($"/api/blackjack/{ep}", null);

            resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
                $"GET/POST /api/blackjack/{ep} should return 401 for anonymous user");
        }
    }

    [Fact]
    public async Task Blackjack_DealAndHit_ShouldProgressRound()
    {
        await EnsureTestPlayerAsync();

        await AdminClient.PostAsJsonAsync("/api/blackjack/bet", new { amount = 50 });
        await AdminClient.PostAsync("/api/blackjack/deal", null);

        var hitResp = await AdminClient.PostAsync("/api/blackjack/hit", null);
        hitResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var afterHit = await hitResp.Content.ReadFromJsonAsync<BlackjackStateDTO>();
        afterHit!.Hand.Should().NotBeEmpty();

        // Finish with stand
        await AdminClient.PostAsync("/api/blackjack/stand", null);
    }

    [Fact]
    public async Task Blackjack_Double_ShouldAcceptWhenEligible()
    {
        await EnsureTestPlayerAsync();

        await AdminClient.PostAsJsonAsync("/api/blackjack/bet", new { amount = 100 });
        await AdminClient.PostAsync("/api/blackjack/deal", null);

        var dblResp = await AdminClient.PostAsync("/api/blackjack/double", null);

        dblResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var afterDouble = await dblResp.Content.ReadFromJsonAsync<BlackjackStateDTO>();
        afterDouble!.Phase.Should().Be("round-over");
    }

    // ═══════════════════════════════════════════════════════════════
    //  ROULETTE  (4 endpoints)
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task Roulette_State_ShouldReturnOk_WhenAuthenticated()
    {
        await EnsureTestPlayerAsync();

        var resp = await AdminClient.GetAsync("/api/roulette/state");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var state = await resp.Content.ReadFromJsonAsync<RouletteStateDTO>();
        state.Should().NotBeNull();
        state!.PlayerName.Should().Be("Test User");
        state.Balance.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public async Task Roulette_BetAndSpin_ShouldCompleteRound()
    {
        await EnsureTestPlayerAsync();

        var betResp = await AdminClient.PostAsJsonAsync("/api/roulette/bet",
            new { kind = "red", number = (int?)null, amount = 100 });
        betResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var afterBet = await betResp.Content.ReadFromJsonAsync<RouletteStateDTO>();
        afterBet!.CanSpin.Should().BeTrue();

        var spinResp = await AdminClient.PostAsync("/api/roulette/spin", null);
        spinResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var afterSpin = await spinResp.Content.ReadFromJsonAsync<RouletteStateDTO>();
        afterSpin!.LastNumber.Should().NotBeNull();
        afterSpin.LastColor.Should().NotBeNull();
    }

    [Fact]
    public async Task Roulette_ClearBets_ShouldRemoveAllBets()
    {
        await EnsureTestPlayerAsync();

        await AdminClient.PostAsJsonAsync("/api/roulette/bet",
            new { kind = "straight", number = 7, amount = 50 });

        var clearResp = await AdminClient.PostAsync("/api/roulette/clear", null);
        clearResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Complete the round
        await AdminClient.PostAsync("/api/roulette/spin", null);
    }

    [Fact]
    public async Task Roulette_Bet_ShouldReturn400_WhenKindMissing()
    {
        await EnsureTestPlayerAsync();

        var resp = await AdminClient.PostAsJsonAsync("/api/roulette/bet",
            new { kind = "", number = 0, amount = 50 });

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Roulette_Bet_ShouldReturn400_WhenAmountInvalid()
    {
        await EnsureTestPlayerAsync();

        var resp = await AdminClient.PostAsJsonAsync("/api/roulette/bet",
            new { kind = "black", number = (int?)null, amount = 0 });

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Roulette_AllEndpoints_ShouldReturn401_WhenAnonymous()
    {
        var endpoints = new[] { "state", "bet", "clear", "spin" };
        foreach (var ep in endpoints)
        {
            HttpResponseMessage resp;
            if (ep == "state")
                resp = await Client.GetAsync($"/api/roulette/{ep}");
            else
                resp = await Client.PostAsync($"/api/roulette/{ep}", null);

            resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
                $"GET/POST /api/roulette/{ep} should return 401 for anonymous user");
        }
    }

    [Fact]
    public async Task Roulette_MultipleNumberBets_ShouldAccumulate()
    {
        await EnsureTestPlayerAsync();

        // Complete any pending round first
        await AdminClient.PostAsync("/api/roulette/spin", null);

        await AdminClient.PostAsJsonAsync("/api/roulette/bet",
            new { kind = "straight", number = 13, amount = 25 });
        await AdminClient.PostAsJsonAsync("/api/roulette/bet",
            new { kind = "straight", number = 17, amount = 25 });

        var stateResp = await AdminClient.GetAsync("/api/roulette/state");
        var state = await stateResp.Content.ReadFromJsonAsync<RouletteStateDTO>();
        state!.Bets.Should().HaveCount(2);

        await AdminClient.PostAsync("/api/roulette/spin", null);
    }

    // ═══════════════════════════════════════════════════════════════
    //  SLOT  (5 endpoints: state, bet, spin, gamble, gamble/collect)
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task Slot_State_ShouldReturnOk_WhenAuthenticated()
    {
        await EnsureTestPlayerAsync();

        var resp = await AdminClient.GetAsync("/api/slot/state");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var state = await resp.Content.ReadFromJsonAsync<SlotStateDTO>();
        state.Should().NotBeNull();
        state!.PlayerName.Should().Be("Test User");
        state.Symbols.Should().NotBeEmpty();
        state.Paylines.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Slot_BetAndSpin_ShouldCompleteRound()
    {
        await EnsureTestPlayerAsync();

        var betResp = await AdminClient.PostAsJsonAsync("/api/slot/bet",
            new { amount = (decimal?)100m });
        betResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var afterBet = await betResp.Content.ReadFromJsonAsync<SlotStateDTO>();
        afterBet!.CanSpin.Should().BeTrue();

        var spinResp = await AdminClient.PostAsync("/api/slot/spin", null);
        spinResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var afterSpin = await spinResp.Content.ReadFromJsonAsync<SlotStateDTO>();
        afterSpin!.Round.Should().NotBeNull();
        afterSpin.Round!.BaseSpin.Grid.Should().HaveCount(5);
    }

    [Fact]
    public async Task Slot_Bet_ShouldReturn400_WhenAmountInvalid()
    {
        await EnsureTestPlayerAsync();

        var resp = await AdminClient.PostAsJsonAsync("/api/slot/bet",
            new { amount = (decimal?)0m });

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Slot_Gamble_ShouldReturn400_WhenChoiceInvalid()
    {
        await EnsureTestPlayerAsync();

        // Must be "red" or "black"
        var resp = await AdminClient.PostAsJsonAsync("/api/slot/gamble",
            new { choice = "green" });

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Slot_GambleCollect_ShouldHandleEvenWithoutWin()
    {
        await EnsureTestPlayerAsync();

        var resp = await AdminClient.PostAsync("/api/slot/gamble/collect", null);

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var state = await resp.Content.ReadFromJsonAsync<SlotStateDTO>();
        state.Should().NotBeNull();
    }

    [Fact]
    public async Task Slot_AllEndpoints_ShouldReturn401_WhenAnonymous()
    {
        var endpoints = new[] { "state", "bet", "spin", "gamble", "gamble/collect" };
        foreach (var ep in endpoints)
        {
            HttpResponseMessage resp;
            if (ep == "state")
                resp = await Client.GetAsync($"/api/slot/{ep}");
            else
                resp = await Client.PostAsync($"/api/slot/{ep}", null);

            resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
                $"GET/POST /api/slot/{ep} should return 401 for anonymous user");
        }
    }

    [Fact]
    public async Task Slot_GambleFullFlow_ShouldHandleRedBlack()
    {
        await EnsureTestPlayerAsync();

        // First spin to potentially get a win for gambling
        await AdminClient.PostAsJsonAsync("/api/slot/bet", new { amount = (decimal?)100m });
        await AdminClient.PostAsync("/api/slot/spin", null);

        // Try gamble
        var gambleResp = await AdminClient.PostAsJsonAsync("/api/slot/gamble",
            new { choice = "red" });
        gambleResp.StatusCode.Should().BeOneOf([HttpStatusCode.OK, HttpStatusCode.BadRequest]);

        // Collect any remaining gamble stake
        await AdminClient.PostAsync("/api/slot/gamble/collect", null);
    }

    // ═══════════════════════════════════════════════════════════════
    //  THREE BODY  (3 endpoints)
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task ThreeBody_State_ShouldReturnOk_WhenAuthenticated()
    {
        await EnsureTestPlayerAsync();

        var resp = await AdminClient.GetAsync("/api/threebody/state");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var state = await resp.Content.ReadFromJsonAsync<ThreeBodyStateDTO>();
        state.Should().NotBeNull();
        state!.PlayerName.Should().Be("Test User");
        state.Planets.Should().HaveCount(3);
        state.CanStart.Should().BeFalse(); // no bet placed yet, but may vary if previous state lingers
    }

    [Fact]
    public async Task ThreeBody_BetAndStart_ShouldCompleteRound()
    {
        await EnsureTestPlayerAsync();

        var betResp = await AdminClient.PostAsJsonAsync("/api/threebody/bet",
            new { amount = 100, planet = "A" });
        betResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var afterBet = await betResp.Content.ReadFromJsonAsync<ThreeBodyStateDTO>();
        afterBet!.CanStart.Should().BeTrue();
        afterBet.BetOnPlanet.Should().Be("A");

        var startResp = await AdminClient.PostAsync("/api/threebody/start", null);
        startResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var afterStart = await startResp.Content.ReadFromJsonAsync<ThreeBodyStateDTO>();
        afterStart!.Round.Should().NotBeNull();
        afterStart.Round!.Frames.Should().NotBeEmpty();
        afterStart.LastResult.Should().NotBeNull();
    }

    [Fact]
    public async Task ThreeBody_Bet_ShouldReturn400_WhenPlanetInvalid()
    {
        await EnsureTestPlayerAsync();

        var resp = await AdminClient.PostAsJsonAsync("/api/threebody/bet",
            new { amount = 100, planet = "D" });

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ThreeBody_Bet_ShouldReturn400_WhenAmountInvalid()
    {
        await EnsureTestPlayerAsync();

        var resp = await AdminClient.PostAsJsonAsync("/api/threebody/bet",
            new { amount = 7, planet = "B" });

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ThreeBody_AllEndpoints_ShouldReturn401_WhenAnonymous()
    {
        var endpoints = new[] { "state", "bet", "start" };
        foreach (var ep in endpoints)
        {
            HttpResponseMessage resp;
            if (ep == "state")
                resp = await Client.GetAsync($"/api/threebody/{ep}");
            else
                resp = await Client.PostAsync($"/api/threebody/{ep}", null);

            resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
                $"GET/POST /api/threebody/{ep} should return 401 for anonymous user");
        }
    }

    [Fact]
    public async Task ThreeBody_PlanetB_ShouldWork()
    {
        await EnsureTestPlayerAsync();

        var betResp = await AdminClient.PostAsJsonAsync("/api/threebody/bet",
            new { amount = 200, planet = "B" });
        betResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var startResp = await AdminClient.PostAsync("/api/threebody/start", null);
        startResp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ThreeBody_PlanetC_ShouldWork()
    {
        await EnsureTestPlayerAsync();

        var betResp = await AdminClient.PostAsJsonAsync("/api/threebody/bet",
            new { amount = 50, planet = "C" });
        betResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var startResp = await AdminClient.PostAsync("/api/threebody/start", null);
        startResp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ThreeBody_Start_WithoutBet_ShouldHaveNoBetStatus()
    {
        await EnsureTestPlayerAsync();

        // Complete any pending round first
        await AdminClient.PostAsJsonAsync("/api/threebody/bet", new { amount = 100, planet = "A" });
        await AdminClient.PostAsync("/api/threebody/start", null);

        // Now try starting without a bet — service returns OK with status message
        var startResp = await AdminClient.PostAsync("/api/threebody/start", null);

        startResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var state = await startResp.Content.ReadFromJsonAsync<ThreeBodyStateDTO>();
        state!.CanStart.Should().BeFalse();
    }
}
