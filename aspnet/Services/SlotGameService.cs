using System;
using System.Collections.Generic;
using System.Linq;
using aspnet.Data;
using aspnet.Models;
using aspnet.Models.DTO;

namespace aspnet.Services;

// Singleplayer slot u stilu klasičnog 5×3 "charm" automata: 10 linija slijeva
// nadesno, Lady je istovremeno wild (zamjenjuje sve osim sebe na liniji) i
// scatter (3+ bilo gdje pokreće feature). Feature = 15 free spinova s istim
// ulogom (besplatni), uz retrigger. Cijela runda se razriješi u jednom pozivu
// na Spin; klijent reproducira snimljene spinove. RTP je namjerno ~120%
// (dobrotvorni casino) — tune-an preko RtpScale, provjereno Monte-Carlo testom.
public class SlotGameService
{
    public const int Reels = 5;
    public const int Rows = 3;
    public const int Lines = 10;
    public const int FreeSpinsPerTrigger = 15;
    private const int ScatterTrigger = 3;
    private const int MaxTotalFreeSpins = 300; // tvrdi limit protiv runaway retriggera

    // Globalni množitelj svih dobitaka kojim se RTP namješta na ~120%.
    // Dobitci skaliraju linearno pa je: RtpScale = 1.20 / izmjereni_bazni_RTP.
    // Vrijednost potvrđena SlotRtpTests (RTP ostaje u [1.15, 1.25]).
    public const decimal RtpScale = 1.7613m;

    private static readonly TimeSpan SessionTimeout = TimeSpan.FromMinutes(30);

    // Ulozi su ukupni; line bet = ukupni / 10 (mora biti djeljiv s 10).
    public static readonly int[] AllowedBets = { 10, 20, 50, 100, 200 };

    // ── Simboli ───────────────────────────────────────────────────────────
    // Indeks 0 = Lady (wild + scatter). Ostalo: 4 "charm" visoka simbola pa
    // 5 niskih (kartaške vrijednosti). Isplate su višekratnici line-beta za
    // 3/4/5 u nizu; scatter (Lady) plaća × ukupni ulog (ScatterPay niže).
    public sealed record SymbolDef(string Key, string Name, bool Wild, bool Scatter, int Weight, int Pay3, int Pay4, int Pay5);

    public static readonly SymbolDef[] Symbols =
    {
        new("lady",      "Lucky Lady", Wild: true,  Scatter: true,  Weight: 2, Pay3: 200, Pay4: 1000, Pay5: 5000),
        new("clover",    "Clover",     Wild: false, Scatter: false, Weight: 4, Pay3: 40,  Pay4: 100,  Pay5: 400),
        new("ladybug",   "Ladybug",    Wild: false, Scatter: false, Weight: 4, Pay3: 40,  Pay4: 100,  Pay5: 400),
        new("horseshoe", "Horseshoe",  Wild: false, Scatter: false, Weight: 5, Pay3: 20,  Pay4: 60,   Pay5: 200),
        new("coin",      "Gold Coin",  Wild: false, Scatter: false, Weight: 5, Pay3: 20,  Pay4: 60,   Pay5: 200),
        new("ace",       "Ace",        Wild: false, Scatter: false, Weight: 6, Pay3: 10,  Pay4: 30,   Pay5: 100),
        new("king",      "King",       Wild: false, Scatter: false, Weight: 7, Pay3: 10,  Pay4: 30,   Pay5: 100),
        new("queen",     "Queen",      Wild: false, Scatter: false, Weight: 7, Pay3: 5,   Pay4: 20,   Pay5: 75),
        new("jack",      "Jack",       Wild: false, Scatter: false, Weight: 8, Pay3: 5,   Pay4: 20,   Pay5: 75),
        new("ten",       "Ten",        Wild: false, Scatter: false, Weight: 8, Pay3: 5,   Pay4: 20,   Pay5: 75),
    };

    private const int LadyIndex = 0;

    // Scatter (Lady, bilo gdje) plaća × ukupni ulog za 3/4/5 komada.
    private static readonly int[] ScatterPay = { 2, 10, 50 }; // [0]=3 scatera, [1]=4, [2]=5

    // 10 linija: redak (0=gore,1=sredina,2=dolje) po svakom od 5 stupaca.
    // Oblici su čisto kozmetički — RTP ovisi samo o broju linija i strip/paytable.
    public static readonly int[][] Paylines =
    {
        new[] { 1, 1, 1, 1, 1 },
        new[] { 0, 0, 0, 0, 0 },
        new[] { 2, 2, 2, 2, 2 },
        new[] { 0, 1, 2, 1, 0 },
        new[] { 2, 1, 0, 1, 2 },
        new[] { 1, 0, 1, 2, 1 },
        new[] { 1, 2, 1, 0, 1 },
        new[] { 0, 0, 1, 0, 0 },
        new[] { 2, 2, 1, 2, 2 },
        new[] { 0, 1, 1, 1, 0 },
    };

    // Virtualni reel (isti za svih 5 stupaca): indeksi simbola ponovljeni po Weightu.
    private static readonly int[] ReelStrip = BuildStrip();

    private static int[] BuildStrip()
    {
        var strip = new List<int>();
        for (var i = 0; i < Symbols.Length; i++)
            for (var w = 0; w < Symbols[i].Weight; w++)
                strip.Add(i);
        return strip.ToArray();
    }

    // ── Čista logika spina/evaluacije (bez DB-a, koristi je i Monte-Carlo test) ──

    // Vrti mrežu: grid[reel][row] = indeks simbola.
    public static int[][] SpinGrid(Random rng)
    {
        var grid = new int[Reels][];
        for (var r = 0; r < Reels; r++)
        {
            grid[r] = new int[Rows];
            for (var row = 0; row < Rows; row++)
                grid[r][row] = ReelStrip[rng.Next(ReelStrip.Length)];
        }
        return grid;
    }

    public readonly record struct LineWin(int Line, int SymbolIndex, int Count, decimal Amount, int[] Cells);

    // Evaluira jednu mrežu. lineBet = ukupni / 10. Vraća dobitke po liniji,
    // scatter dobitak i broj scattera (za feature). RtpScale je već uračunat.
    public static (List<LineWin> LineWins, decimal ScatterWin, int ScatterCount, decimal Total)
        Evaluate(int[][] grid, decimal totalBet)
    {
        var lineBet = totalBet / Lines;
        var lineWins = new List<LineWin>();
        decimal total = 0;

        for (var l = 0; l < Paylines.Length; l++)
        {
            var rows = Paylines[l];
            var lineSymbols = new int[Reels];
            for (var r = 0; r < Reels; r++) lineSymbols[r] = grid[r][rows[r]];

            decimal bestPay = 0;
            var bestSymbol = -1;
            var bestCount = 0;

            // Za svaki ne-scatter simbol: koliko vodećih stupaca je taj simbol
            // ILI Lady (wild). Lady kao vlastiti linijski simbol obrađen je
            // pod indeksom 0 (vodeći Lady), pa max pokriva i čisto-Lady liniju.
            for (var s = 0; s < Symbols.Length; s++)
            {
                var count = 0;
                for (var r = 0; r < Reels; r++)
                {
                    var cell = lineSymbols[r];
                    var matches = cell == s || (s != LadyIndex && cell == LadyIndex);
                    if (!matches) break;
                    count++;
                }
                if (count < 3) continue;
                var pay = PayFor(s, count) * lineBet * RtpScale;
                if (pay > bestPay)
                {
                    bestPay = pay;
                    bestSymbol = s;
                    bestCount = count;
                }
            }

            if (bestPay > 0)
            {
                var cells = rows.Take(bestCount).ToArray();
                lineWins.Add(new LineWin(l + 1, bestSymbol, bestCount, bestPay, cells));
                total += bestPay;
            }
        }

        // Scatter: broj Lady simbola bilo gdje u mreži.
        var scatterCount = 0;
        for (var r = 0; r < Reels; r++)
            for (var row = 0; row < Rows; row++)
                if (grid[r][row] == LadyIndex) scatterCount++;

        decimal scatterWin = 0;
        if (scatterCount >= 3)
        {
            scatterWin = ScatterPay[Math.Min(scatterCount, 5) - 3] * totalBet * RtpScale;
            total += scatterWin;
        }

        return (lineWins, scatterWin, scatterCount, total);
    }

    private static int PayFor(int symbol, int count) => count switch
    {
        3 => Symbols[symbol].Pay3,
        4 => Symbols[symbol].Pay4,
        5 => Symbols[symbol].Pay5,
        _ => 0
    };

    // Razriješi cijelu rundu: osnovni spin + svi free spinovi (s retriggerom).
    // Vraća snimljene spinove i ukupni dobitak. Ne dira DB.
    public static (List<SlotSpinDTO> Spins, int FreeAwarded, bool Triggered, decimal BaseWin, decimal FreeWin)
        ResolveRound(Random rng, decimal totalBet)
    {
        var spins = new List<SlotSpinDTO>();

        var grid = SpinGrid(rng);
        var (lineWins, scatterWin, scatterCount, baseTotal) = Evaluate(grid, totalBet);
        spins.Add(BuildSpin(grid, lineWins, scatterWin, scatterCount, baseTotal, free: false));

        var triggered = scatterCount >= ScatterTrigger;
        var remaining = triggered ? FreeSpinsPerTrigger : 0;
        var awarded = remaining;
        decimal freeWin = 0;

        while (remaining > 0 && awarded <= MaxTotalFreeSpins)
        {
            remaining--;
            var fg = SpinGrid(rng);
            var (fl, fsc, fScatter, fTotal) = Evaluate(fg, totalBet);
            spins.Add(BuildSpin(fg, fl, fsc, fScatter, fTotal, free: true));
            freeWin += fTotal;

            // Retrigger: 3+ Lady tijekom free spina dodaje još 15 (do limita).
            if (fScatter >= ScatterTrigger && awarded < MaxTotalFreeSpins)
            {
                var add = Math.Min(FreeSpinsPerTrigger, MaxTotalFreeSpins - awarded);
                remaining += add;
                awarded += add;
            }
        }

        return (spins, awarded, triggered, baseTotal, freeWin);
    }

    private static SlotSpinDTO BuildSpin(int[][] grid, List<LineWin> lineWins, decimal scatterWin, int scatterCount, decimal total, bool free)
    {
        var keyGrid = new string[Reels][];
        for (var r = 0; r < Reels; r++)
        {
            keyGrid[r] = new string[Rows];
            for (var row = 0; row < Rows; row++)
                keyGrid[r][row] = Symbols[grid[r][row]].Key;
        }

        var lineDtos = lineWins
            .Select(w => new SlotLineWinDTO(w.Line, Symbols[w.SymbolIndex].Key, w.Count, Round2(w.Amount), w.Cells))
            .ToList();

        return new SlotSpinDTO(keyGrid, lineDtos, scatterCount, Round2(scatterWin), Round2(total), free);
    }

    private static decimal Round2(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);

    // ── Stanje po igraču + DB ───────────────────────────────────────────────

    private sealed class GameSession
    {
        public DateTime LastSeenUtc;
        public long Version;
        public string Status = "Pick your bet and spin the charms.";
        public int SelectedBet = AllowedBets[0];
        public decimal LastWin;
        public int Spins;
        public int FeatureHits;
    }

    private readonly object _sync = new();
    private readonly Random _rng = new();
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly Dictionary<int, GameSession> _sessions = new();

    public SlotGameService(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    public SlotStateDTO GetState(int playerId) => Mutate(playerId, (_, _, _) => { });

    public SlotStateDTO SetBet(int playerId, int amount)
        => Mutate(playerId, (s, _, p) =>
        {
            if (!AllowedBets.Contains(amount))
            {
                s.Status = "Invalid bet amount.";
                return;
            }
            s.SelectedBet = amount;
            s.Status = amount > p.Balance
                ? "Bet too high for your balance."
                : $"Bet set to ${amount}. Spin to play.";
        });

    public SlotStateDTO Spin(int playerId)
    {
        SlotRoundDTO? round = null;

        var state = Mutate(playerId, (s, db, p) =>
        {
            if (s.SelectedBet > p.Balance)
            {
                s.Status = "Insufficient balance for this bet.";
                return;
            }

            p.Balance -= s.SelectedBet;
            AddTransaction(db, p.Id, TransactionType.Bet, s.SelectedBet);

            var (spins, awarded, triggered, baseWin, freeWin) = ResolveRound(_rng, s.SelectedBet);
            var totalWin = Round2(baseWin + freeWin);

            if (totalWin > 0)
            {
                p.Balance += totalWin;
                AddTransaction(db, p.Id, TransactionType.Win, totalWin);
            }

            s.Spins++;
            if (triggered) s.FeatureHits++;
            s.LastWin = totalWin;

            s.Status = triggered
                ? $"Feature! {awarded} free spins paid ${totalWin:0.##}."
                : totalWin > 0
                    ? $"You won ${totalWin:0.##}!"
                    : "No win this time. Spin again!";

            round = new SlotRoundDTO(
                Spins: spins,
                FreeSpinsAwarded: triggered ? awarded : 0,
                FeatureTriggered: triggered,
                BaseWin: Round2(baseWin),
                FreeWin: Round2(freeWin),
                TotalWin: totalWin,
                BetAmount: s.SelectedBet,
                Result: totalWin > 0 ? "win" : "loss");
        });

        return state with { Round = round };
    }

    private SlotStateDTO Mutate(int playerId, Action<GameSession, CasinoDbContext, Player> action)
    {
        lock (_sync)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CasinoDbContext>();

            var now = DateTime.UtcNow;
            foreach (var stale in _sessions.Where(kv => now - kv.Value.LastSeenUtc > SessionTimeout).ToList())
                _sessions.Remove(stale.Key);

            if (!_sessions.TryGetValue(playerId, out var session))
            {
                session = new GameSession();
                _sessions[playerId] = session;
            }
            session.LastSeenUtc = now;

            var player = db.Players.Find(playerId)
                ?? throw new InvalidOperationException($"Player {playerId} not found.");

            action(session, db, player);

            db.SaveChanges();
            session.Version++;
            return BuildView(session, player);
        }
    }

    private static SlotStateDTO BuildView(GameSession s, Player p)
        => new(
            Version: s.Version,
            Status: s.Status,
            PlayerName: $"{p.FirstName} {p.LastName}".Trim(),
            Balance: p.Balance,
            SelectedBet: s.SelectedBet,
            AllowedBets: AllowedBets,
            Symbols: Symbols
                .Select(sym => new SlotSymbolDTO(sym.Key, sym.Name, sym.Wild, sym.Scatter, sym.Pay3, sym.Pay4, sym.Pay5))
                .ToList(),
            Paylines: Paylines,
            Reels: Reels,
            Rows: Rows,
            Lines: Lines,
            LastWin: s.LastWin,
            Spins: s.Spins,
            FeatureHits: s.FeatureHits,
            CanSpin: s.SelectedBet <= p.Balance,
            Round: null);

    private static void AddTransaction(CasinoDbContext db, int playerId, TransactionType type, decimal amount)
    {
        db.Transactions.Add(new Transaction
        {
            PlayerId = playerId,
            Type = type,
            Amount = amount,
            CreatedAt = DateTime.UtcNow
        });
    }
}
