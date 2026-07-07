using aspnet.Services;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace aspnet.Tests;

// Monte-Carlo provjera da je slot RTP ~120% (dobrotvorni casino), i za
// prirodnu igru i za Feature Buy. RtpScale (linijski dobitci) i
// FeatureBuyMultiplier u SlotGameService su namješteni da padnu u [1.15, 1.25].
public class SlotRtpTests
{
    private readonly ITestOutputHelper _out;
    public SlotRtpTests(ITestOutputHelper output) => _out = output;

    [Fact]
    public void Rtp_is_close_to_120_percent()
    {
        const int rounds = 3_000_000;
        const decimal bet = 100m;
        var rng = new Random(12345);

        decimal wagered = 0;
        decimal lineReturned = 0;
        decimal bonusReturned = 0;
        var featureHits = 0;
        var winningRounds = 0;

        for (var i = 0; i < rounds; i++)
        {
            wagered += bet;
            var (_, bonus, baseWin, bonusWin) = SlotGameService.ResolveRound(rng, bet);
            lineReturned += baseWin;
            bonusReturned += bonusWin;
            if (bonus is not null) featureHits++;
            if (baseWin + bonusWin > 0) winningRounds++;
        }

        var rtp = (lineReturned + bonusReturned) / wagered;
        var featureRate = (double)featureHits / rounds;
        var hitRate = (double)winningRounds / rounds;
        _out.WriteLine($"RTP = {rtp:P3} (lines {lineReturned / wagered:P3} + bonus {bonusReturned / wagered:P3}) | hit rate = {hitRate:P1} | feature every {1 / featureRate:0} spins | RtpScale = {SlotGameService.RtpScale}");

        rtp.Should().BeInRange(1.15m, 1.25m);
    }

    // Osnovna igra treba davati česte male dobitke — bez dugih suhih serija.
    [Fact]
    public void Base_game_hits_frequently()
    {
        const int rounds = 500_000;
        const decimal bet = 100m;
        var rng = new Random(777);

        var winningRounds = 0;
        for (var i = 0; i < rounds; i++)
        {
            var (_, bonus, baseWin, bonusWin) = SlotGameService.ResolveRound(rng, bet);
            if (baseWin + bonusWin > 0 || bonus is not null) winningRounds++;
        }

        var hitRate = (double)winningRounds / rounds;
        _out.WriteLine($"hit rate = {hitRate:P1}");
        hitRate.Should().BeGreaterThan(0.25);
    }

    [Fact]
    public void Feature_buy_rtp_is_close_to_120_percent()
    {
        const int rounds = 300_000;
        const decimal bet = 100m;
        var rng = new Random(54321);

        decimal wagered = 0;
        decimal returned = 0;
        var fullGrids = 0;
        var grands = 0;

        for (var i = 0; i < rounds; i++)
        {
            wagered += bet * SlotGameService.FeatureBuyMultiplier;
            var (_, bonus, baseWin, bonusWin) = SlotGameService.ResolveRound(rng, bet, buyFeature: true);
            returned += baseWin + bonusWin;
            bonus.Should().NotBeNull("Feature Buy must always trigger the bonus");
            if (bonus!.FullGrid) fullGrids++;
            grands += bonus.JackpotsWon.Count(j => j == "grand");
        }

        var rtp = returned / wagered;
        _out.WriteLine($"buy RTP = {rtp:P3} | full grid every {rounds / Math.Max(1, fullGrids):0} buys | grand every {rounds / Math.Max(1, grands):0} buys | cost = {SlotGameService.FeatureBuyMultiplier}× bet");

        rtp.Should().BeInRange(1.10m, 1.30m);
    }

    // Mehanika bonusa: nova kugla resetira brojač na 3, kraj na 0 respinova
    // ili punoj mreži; isplata je točno zbroj svih zaključanih kugli.
    [Fact]
    public void Bonus_respin_bookkeeping_is_consistent()
    {
        const decimal bet = 10m;
        var rng = new Random(99);

        for (var i = 0; i < 20_000; i++)
        {
            var (_, bonus, _, _) = SlotGameService.ResolveRound(rng, bet, buyFeature: true);
            bonus.Should().NotBeNull();

            var locked = bonus!.InitialCharms.Count;
            var remaining = SlotGameService.RespinsPerLock;
            decimal sum = bonus.InitialCharms.Sum(c => c.Amount);

            foreach (var respin in bonus.Respins)
            {
                remaining--;
                locked += respin.NewCharms.Count;
                if (respin.NewCharms.Count > 0) remaining = SlotGameService.RespinsPerLock;

                respin.LockedCount.Should().Be(locked);
                respin.RespinsLeft.Should().Be(remaining);
                sum += respin.NewCharms.Sum(c => c.Amount);
            }

            (remaining == 0 || locked == 15).Should().BeTrue("bonus must end at 0 respins or a full grid");
            bonus.FullGrid.Should().Be(locked == 15);
            bonus.TotalWin.Should().Be(Math.Round(sum, 2, MidpointRounding.AwayFromZero));
        }
    }
}
