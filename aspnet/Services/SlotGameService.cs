using System;
using System.Collections.Generic;
using System.Linq;
using aspnet.Data;
using aspnet.Models;
using aspnet.Models.DTO;

namespace aspnet.Services;

// Singleplayer slot u stilu modernog "Hold & Win" automata: 5×3, 10 linija
// slijeva nadesno. Lady je čisti wild (zamjenjuje sve osim Charm Ball kugle);
// Charm Ball je scatter — svaka kugla nosi cash vrijednost ili Mini/Major/
// Grand jackpot, a 3+ bilo gdje (ili Feature Buy) pokreće Hold & Win bonus:
// okidačke kugle se zaključaju, kreće se s 3 respina, svaka nova kugla se
// zaključa i resetira brojač na 3, puna mreža (15/15) kupi sve odjednom.
// Cijela runda se razriješi u jednom pozivu na Spin/Buy; klijent reproducira
// snimljene spinove. RTP je namjerno ~120% (dobrotvorni casino) — tune-an
// preko RtpScale, provjereno Monte-Carlo testom.
public class SlotGameService
{
    public const int Reels = 5;
    public const int Rows = 3;
    public const int Lines = 10;
    public const int RespinsPerLock = 3;      // brojač respinova; svaka nova kugla ga resetira
    private const int ScatterTrigger = 3;
    private const int GridCells = Reels * Rows;

    // Množitelj linijskih dobitaka kojim se ukupni RTP namješta na ~120%.
    // Bonus (Charm Ball) isplate su čisti višekratnici uloga i NE skaliraju se,
    // da jackpoti i cash vrijednosti ostanu okrugli iznosi. Vrijednost
    // potvrđena SlotRtpTests (ukupni RTP ostaje u [1.15, 1.25]).
    public const decimal RtpScale = 1.32m;

    // Jackpoti su fiksni višekratnici ukupnog uloga.
    public const decimal MiniJackpot = 20m;
    public const decimal MajorJackpot = 100m;
    public const decimal GrandJackpot = 1000m;

    // Feature Buy: cijena u višekratnicima ukupnog uloga. Namještena tako da
    // je RTP kupljenog bonusa (~3 početne kugle) blizu 120%, kao i osnovna igra.
    public const decimal FeatureBuyMultiplier = 16m;

    private static readonly TimeSpan SessionTimeout = TimeSpan.FromMinutes(30);

    // Ulozi su ukupni (raspon $0.10–$100); line bet = ukupni / 10.
    public static readonly decimal[] AllowedBets =
        { 0.10m, 0.20m, 0.50m, 1m, 2m, 5m, 10m, 20m, 50m, 100m };
    private const decimal DefaultBet = 1m;

    // ── Simboli ───────────────────────────────────────────────────────────
    // Indeks 0 = Lady (wild), indeks 1 = Charm Ball (scatter, ne plaća linije).
    // Ostalo: 4 premium "charm" simbola pa 5 niskih (kartaške vrijednosti).
    // Isplate su višekratnici line-beta za 3/4/5 u nizu. Težine su nagnute
    // prema niskim simbolima da osnovna igra daje česte male dobitke.
    public sealed record SymbolDef(string Key, string Name, bool Wild, bool Scatter, int Weight, int Pay3, int Pay4, int Pay5);

    public static readonly SymbolDef[] Symbols =
    {
        new("lady",      "Lucky Lady",  Wild: true,  Scatter: false, Weight: 4, Pay3: 100, Pay4: 500, Pay5: 2000),
        new("charm",     "Charm Ball",  Wild: false, Scatter: true,  Weight: 3, Pay3: 0,   Pay4: 0,   Pay5: 0),
        new("potofgold", "Pot of Gold", Wild: false, Scatter: false, Weight: 3, Pay3: 50,  Pay4: 150, Pay5: 500),
        new("clover",    "Four-Leaf Clover", Wild: false, Scatter: false, Weight: 4, Pay3: 30, Pay4: 100, Pay5: 300),
        new("horseshoe", "Horseshoe",   Wild: false, Scatter: false, Weight: 5, Pay3: 25,  Pay4: 75,  Pay5: 200),
        new("bell",      "Golden Bell", Wild: false, Scatter: false, Weight: 5, Pay3: 20,  Pay4: 60,  Pay5: 150),
        new("ace",       "Ace",         Wild: false, Scatter: false, Weight: 8, Pay3: 10,  Pay4: 25,  Pay5: 75),
        new("king",      "King",        Wild: false, Scatter: false, Weight: 8, Pay3: 10,  Pay4: 25,  Pay5: 75),
        new("queen",     "Queen",       Wild: false, Scatter: false, Weight: 10, Pay3: 5,  Pay4: 15,  Pay5: 50),
        new("jack",      "Jack",        Wild: false, Scatter: false, Weight: 10, Pay3: 5,  Pay4: 15,  Pay5: 50),
        new("ten",       "Ten",         Wild: false, Scatter: false, Weight: 11, Pay3: 5,  Pay4: 15,  Pay5: 50),
    };

    private const int LadyIndex = 0;
    private const int CharmIndex = 1;

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

    // ── Charm Ball vrijednosti ────────────────────────────────────────────
    // Svaka kugla nosi cash (× ukupni ulog) ili jackpot. Težinska tablica;
    // ista se koristi za kugle u osnovnoj igri (prikaz) i u bonusu (isplata).
    private sealed record CharmPrize(decimal CashMultiplier, string? Jackpot, int Weight);

    private static readonly CharmPrize[] CharmPrizes =
    {
        new(0.5m,  null,    500),
        new(1m,    null,    500),
        new(1.5m,  null,    300),
        new(2m,    null,    240),
        new(3m,    null,    160),
        new(5m,    null,    100),
        new(10m,   null,     40),
        new(MiniJackpot,  "mini",  40),
        new(MajorJackpot, "major", 10),
        new(GrandJackpot, "grand",  1),
    };

    private static readonly int CharmPrizeTotalWeight = CharmPrizes.Sum(p => p.Weight);

    // Vjerojatnost da tijekom bonusa prazna ćelija u jednom respinu dobije
    // kuglu (po ćeliji, nezavisno). Određuje duljinu bonusa i šansu pune mreže.
    private const double BonusCellHitChance = 0.045;

    private static SlotCharmDTO DrawCharm(Random rng, int reel, int row, decimal totalBet)
    {
        var pick = rng.Next(CharmPrizeTotalWeight);
        foreach (var prize in CharmPrizes)
        {
            pick -= prize.Weight;
            if (pick < 0)
                return new SlotCharmDTO(reel, row, Round2(prize.CashMultiplier * totalBet), prize.Jackpot);
        }
        throw new InvalidOperationException("Charm prize table exhausted."); // nedostižno
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

    // Evaluira linijske dobitke jedne mreže. lineBet = ukupni / 10. Charm Ball
    // ne sudjeluje u linijama (scatter); Lady mijenja sve ostale simbole.
    // RtpScale je već uračunat u iznose.
    public static (List<LineWin> LineWins, int ScatterCount, decimal Total)
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

            // Za svaki plativi simbol: koliko vodećih stupaca je taj simbol
            // ILI Lady (wild). Lady kao vlastiti linijski simbol obrađen je
            // pod indeksom 0 (vodeći Lady), pa max pokriva i čisto-Lady liniju.
            for (var s = 0; s < Symbols.Length; s++)
            {
                if (s == CharmIndex) continue;
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

        // Scatter: broj Charm Ball kugli bilo gdje u mreži.
        var scatterCount = 0;
        for (var r = 0; r < Reels; r++)
            for (var row = 0; row < Rows; row++)
                if (grid[r][row] == CharmIndex) scatterCount++;

        return (lineWins, scatterCount, total);
    }

    private static int PayFor(int symbol, int count) => count switch
    {
        3 => Symbols[symbol].Pay3,
        4 => Symbols[symbol].Pay4,
        5 => Symbols[symbol].Pay5,
        _ => 0
    };

    // Razriješi cijelu rundu: osnovni spin + eventualni Hold & Win bonus.
    // buyFeature forsira okidanje s točno 3 kugle (Feature Buy). Ne dira DB.
    public static (SlotSpinDTO BaseSpin, SlotBonusDTO? Bonus, decimal BaseWin, decimal BonusWin)
        ResolveRound(Random rng, decimal totalBet, bool buyFeature = false)
    {
        var grid = SpinGrid(rng);

        if (buyFeature)
        {
            // Očisti prirodne kugle pa usadi točno 3 na slučajne pozicije,
            // da je EV kupljenog bonusa neovisan o sreći osnovnog spina.
            for (var r = 0; r < Reels; r++)
                for (var row = 0; row < Rows; row++)
                    if (grid[r][row] == CharmIndex)
                        grid[r][row] = ReelStripNonCharm(rng);

            foreach (var cell in PickDistinctCells(rng, ScatterTrigger))
                grid[cell / Rows][cell % Rows] = CharmIndex;
        }

        var (lineWins, scatterCount, baseTotal) = Evaluate(grid, totalBet);

        // Svaka kugla u mreži dobije vrijednost/jackpot — kod <3 čista
        // kozmetika, kod 3+ upravo te kugle ulaze u bonus kao zaključane.
        var charms = new List<SlotCharmDTO>();
        for (var r = 0; r < Reels; r++)
            for (var row = 0; row < Rows; row++)
                if (grid[r][row] == CharmIndex)
                    charms.Add(DrawCharm(rng, r, row, totalBet));

        var baseSpin = BuildSpin(grid, lineWins, scatterCount, charms, baseTotal);

        SlotBonusDTO? bonus = null;
        if (scatterCount >= ScatterTrigger)
            bonus = ResolveBonus(rng, totalBet, charms);

        return (baseSpin, bonus, baseTotal, bonus?.TotalWin ?? 0m);
    }

    // Hold & Win: početne kugle su zaključane; 3 respina, svaka nova kugla
    // (bilo koja ćelija, nezavisno BonusCellHitChance) se zaključa i resetira
    // brojač na 3. Kraj kad brojač padne na 0 ili se popuni svih 15 ćelija.
    // Isplata = zbroj svih kugli (puna mreža time "kupi sve" odjednom).
    private static SlotBonusDTO ResolveBonus(Random rng, decimal totalBet, List<SlotCharmDTO> initial)
    {
        var locked = new SlotCharmDTO?[Reels, Rows];
        foreach (var c in initial) locked[c.Reel, c.Row] = c;
        var lockedCount = initial.Count;

        var respins = new List<SlotRespinDTO>();
        var remaining = RespinsPerLock;

        while (remaining > 0 && lockedCount < GridCells)
        {
            remaining--;
            var landed = new List<SlotCharmDTO>();
            for (var r = 0; r < Reels; r++)
                for (var row = 0; row < Rows; row++)
                {
                    if (locked[r, row] is not null) continue;
                    if (rng.NextDouble() >= BonusCellHitChance) continue;
                    var charm = DrawCharm(rng, r, row, totalBet);
                    locked[r, row] = charm;
                    landed.Add(charm);
                }

            lockedCount += landed.Count;
            if (landed.Count > 0) remaining = RespinsPerLock;

            respins.Add(new SlotRespinDTO(landed, remaining, lockedCount));
        }

        var all = new List<SlotCharmDTO>();
        for (var r = 0; r < Reels; r++)
            for (var row = 0; row < Rows; row++)
                if (locked[r, row] is { } c)
                    all.Add(c);

        return new SlotBonusDTO(
            InitialCharms: initial,
            Respins: respins,
            TotalWin: Round2(all.Sum(c => c.Amount)),
            FullGrid: lockedCount == GridCells,
            JackpotsWon: all.Where(c => c.Jackpot is not null).Select(c => c.Jackpot!).ToList());
    }

    // Slučajni ne-charm simbol sa stripa (za čišćenje mreže kod Feature Buya).
    private static int ReelStripNonCharm(Random rng)
    {
        int s;
        do s = ReelStrip[rng.Next(ReelStrip.Length)];
        while (s == CharmIndex);
        return s;
    }

    private static IEnumerable<int> PickDistinctCells(Random rng, int count)
        => Enumerable.Range(0, GridCells).OrderBy(_ => rng.Next()).Take(count);

    private static SlotSpinDTO BuildSpin(int[][] grid, List<LineWin> lineWins, int scatterCount, List<SlotCharmDTO> charms, decimal total)
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

        return new SlotSpinDTO(keyGrid, lineDtos, scatterCount, charms, Round2(total));
    }

    private static decimal Round2(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);

    // ── Stanje po igraču + DB ───────────────────────────────────────────────

    private sealed class GameSession
    {
        public DateTime LastSeenUtc;
        public long Version;
        public string Status = "Pick your bet and spin the charms.";
        public decimal SelectedBet = DefaultBet;
        public decimal LastWin;
        public int Spins;
        public int FeatureHits;

        // Red/black gamble: Offer = zadnji dobitak dostupan za gamble; prvi
        // pick ga skida s balansa u Stake. Pogodak duplira Stake, promašaj
        // gubi sve; Collect vraća Stake na balans.
        public decimal GambleOffer;
        public decimal GambleStake;
        public bool GambleActive;
        public int GambleStep;
        public SlotGambleCardDTO? GambleLastCard;
        public string? GambleLastPick;
        public bool? GambleLastWon;
        public List<SlotGambleCardDTO> GambleHistory = new();

        public void ResetGamble()
        {
            GambleOffer = 0;
            GambleStake = 0;
            GambleActive = false;
            GambleStep = 0;
            GambleLastCard = null;
            GambleLastPick = null;
            GambleLastWon = null;
            GambleHistory = new List<SlotGambleCardDTO>();
        }
    }

    private readonly object _sync = new();
    private readonly Random _rng = new();
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly Dictionary<int, GameSession> _sessions = new();

    public SlotGameService(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    public SlotStateDTO GetState(int playerId) => Mutate(playerId, (_, _, _) => { });

    public SlotStateDTO SetBet(int playerId, decimal amount)
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
                : $"Bet set to ${amount:0.00}. Spin to play.";
        });

    public SlotStateDTO Spin(int playerId) => PlayRound(playerId, buyFeature: false);

    // Feature Buy: plati FeatureBuyMultiplier × ulog i odmah pokreni bonus
    // s 3 zajamčene Charm Ball kugle (linijski dobitci osnovnog spina vrijede).
    public SlotStateDTO BuyFeature(int playerId) => PlayRound(playerId, buyFeature: true);

    private SlotStateDTO PlayRound(int playerId, bool buyFeature)
    {
        SlotRoundDTO? round = null;

        var state = Mutate(playerId, (s, db, p) =>
        {
            if (s.GambleActive)
            {
                s.Status = "Finish your gamble first — collect or pick a color.";
                return;
            }

            var cost = buyFeature ? s.SelectedBet * FeatureBuyMultiplier : s.SelectedBet;
            if (cost > p.Balance)
            {
                s.Status = buyFeature
                    ? "Insufficient balance for the Feature Buy."
                    : "Insufficient balance for this bet.";
                return;
            }

            s.ResetGamble(); // novi spin poništava neiskorištenu gamble ponudu
            p.Balance -= cost;
            AddTransaction(db, p.Id, TransactionType.Bet, cost);

            var (baseSpin, bonus, baseWin, bonusWin) = ResolveRound(_rng, s.SelectedBet, buyFeature);
            var totalWin = Round2(baseWin + bonusWin);
            var triggered = bonus is not null;

            if (totalWin > 0)
            {
                p.Balance += totalWin;
                AddTransaction(db, p.Id, TransactionType.Win, totalWin);
            }

            s.Spins++;
            if (triggered) s.FeatureHits++;
            s.LastWin = totalWin;
            s.GambleOffer = totalWin; // dobitak se može gambleati (red/black)

            s.Status = bonus is not null
                ? bonus.FullGrid
                    ? $"FULL GRID! Every charm collected — ${totalWin:0.##}!"
                    : bonus.JackpotsWon.Count > 0
                        ? $"{string.Join(" + ", bonus.JackpotsWon.Select(Cap))} jackpot! Bonus paid ${totalWin:0.##}."
                        : $"Charm Bonus! {bonus.InitialCharms.Count + bonus.Respins.Sum(r => r.NewCharms.Count)} charms paid ${totalWin:0.##}."
                : totalWin > 0
                    ? $"You won ${totalWin:0.##}!"
                    : "No win this time. Spin again!";

            round = new SlotRoundDTO(
                BaseSpin: baseSpin,
                Bonus: bonus,
                FeatureTriggered: triggered,
                FeatureBought: buyFeature,
                BaseWin: Round2(baseWin),
                BonusWin: Round2(bonusWin),
                TotalWin: totalWin,
                BetAmount: s.SelectedBet,
                Result: totalWin > 0 ? "win" : "loss");
        });

        return state with { Round = round };
    }

    // ── Red/black gamble (double or nothing) ───────────────────────────────

    private static readonly string[] GambleRanks =
        { "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K", "A" };
    private static readonly string[] GambleSuits = { "hearts", "diamonds", "spades", "clubs" };

    // Prvi pick prebacuje ponudu (zadnji dobitak) s balansa u ulog; svaki
    // pogodak boje duplira ulog, promašaj gubi sve. Fer 50/50, EV-neutralno.
    public SlotStateDTO Gamble(int playerId, string choice)
        => Mutate(playerId, (s, db, p) =>
        {
            var pick = choice?.Trim().ToLowerInvariant();
            if (pick is not ("red" or "black"))
            {
                s.Status = "Pick red or black.";
                return;
            }

            if (!s.GambleActive)
            {
                if (s.GambleOffer <= 0)
                {
                    s.Status = "Nothing to gamble — win a spin first.";
                    return;
                }
                if (s.GambleOffer > p.Balance)
                {
                    s.Status = "Balance too low to stake the gamble.";
                    return;
                }
                p.Balance -= s.GambleOffer;
                AddTransaction(db, p.Id, TransactionType.Bet, s.GambleOffer);
                s.GambleStake = s.GambleOffer;
                s.GambleOffer = 0;
                s.GambleActive = true;
            }

            var suit = GambleSuits[_rng.Next(GambleSuits.Length)];
            var card = new SlotGambleCardDTO(
                GambleRanks[_rng.Next(GambleRanks.Length)],
                suit,
                suit is "hearts" or "diamonds" ? "red" : "black");

            s.GambleStep++;
            s.GambleLastCard = card;
            s.GambleLastPick = pick;
            s.GambleHistory.Add(card);

            if (card.Color == pick)
            {
                s.GambleLastWon = true;
                s.GambleStake = Round2(s.GambleStake * 2);
                s.Status = $"{Cap(card.Color)} — correct! ${s.GambleStake:0.00} on the table. Double or collect?";
            }
            else
            {
                s.GambleLastWon = false;
                var lost = s.GambleStake;
                s.GambleStake = 0;
                s.GambleActive = false;
                s.Status = $"{Cap(card.Color)} — wrong. ${lost:0.00} gone. Spin again!";
            }
        });

    public SlotStateDTO CollectGamble(int playerId)
        => Mutate(playerId, (s, db, p) =>
        {
            if (!s.GambleActive || s.GambleStake <= 0)
            {
                s.Status = "No gamble to collect.";
                return;
            }
            var amount = s.GambleStake;
            p.Balance += amount;
            AddTransaction(db, p.Id, TransactionType.Win, amount);
            s.LastWin = amount;
            s.ResetGamble();
            s.Status = $"Collected ${amount:0.00}. Spin to play.";
        });

    private static string Cap(string s) => char.ToUpperInvariant(s[0]) + s[1..];

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
    {
        var buyCost = Round2(s.SelectedBet * FeatureBuyMultiplier);
        return new(
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
            Jackpots: new SlotJackpotsDTO(
                Mini: Round2(MiniJackpot * s.SelectedBet),
                Major: Round2(MajorJackpot * s.SelectedBet),
                Grand: Round2(GrandJackpot * s.SelectedBet)),
            FeatureBuyCost: buyCost,
            CanBuy: buyCost <= p.Balance && !s.GambleActive,
            LastWin: s.LastWin,
            Spins: s.Spins,
            FeatureHits: s.FeatureHits,
            CanSpin: s.SelectedBet <= p.Balance && !s.GambleActive,
            Gamble: new SlotGambleDTO(
                Offer: s.GambleOffer,
                Stake: s.GambleStake,
                Active: s.GambleActive,
                Step: s.GambleStep,
                LastCard: s.GambleLastCard,
                LastPick: s.GambleLastPick,
                LastWon: s.GambleLastWon,
                History: s.GambleHistory.ToList()),
            Round: null);
    }

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
