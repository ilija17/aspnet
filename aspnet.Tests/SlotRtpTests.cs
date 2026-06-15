using aspnet.Services;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace aspnet.Tests;

// Monte-Carlo provjera da je slot RTP ~120% (dobrotvorni casino). Ulog se broji
// samo na plaćeni (osnovni) spin; free spinovi su besplatni pa njihov dobitak
// diže efektivni RTP. RtpScale u SlotGameService je namješten da padne ovdje u [1.15, 1.25].
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
        decimal returned = 0;
        var featureHits = 0;

        for (var i = 0; i < rounds; i++)
        {
            wagered += bet;
            var (_, _, triggered, baseWin, freeWin) = SlotGameService.ResolveRound(rng, bet);
            returned += baseWin + freeWin;
            if (triggered) featureHits++;
        }

        var rtp = returned / wagered;
        var featureRate = (double)featureHits / rounds;
        _out.WriteLine($"RTP = {rtp:P3}  | feature every {1 / featureRate:0} spins  | RtpScale = {SlotGameService.RtpScale}");

        rtp.Should().BeInRange(1.15m, 1.25m);
    }
}
