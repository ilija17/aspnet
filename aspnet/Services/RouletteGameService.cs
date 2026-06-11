using aspnet.Data;
using aspnet.Models;
using aspnet.Models.DTO;

namespace aspnet.Services;

// Server-side europski rulet (jedna nula): oklade, vrtnja i isplate žive
// ovdje — klijent (wwwroot/rulet) je samo GUI koji zove API i crta stanje.
// Oklade se skupljaju u memoriji po igraču; novac se skida tek na Spin
// (jedna Bet transakcija za ukupni ulog, jedna Win za isplatu).
public class RouletteGameService
{
    private static readonly int[] AllowedChips = { 25, 50, 100, 200 };
    private const int MaxBetsPerSpin = 20;
    private static readonly TimeSpan SessionTimeout = TimeSpan.FromMinutes(30);

    private static readonly HashSet<int> RedNumbers = new()
    {
        1, 3, 5, 7, 9, 12, 14, 16, 18, 19, 21, 23, 25, 27, 30, 32, 34, 36
    };

    // Kind → (isplata kao multiplikator uloga s ulogom uključenim, labela)
    private static readonly Dictionary<string, (int Payout, string Label)> OutsideBets = new()
    {
        ["red"] = (2, "Red"),
        ["black"] = (2, "Black"),
        ["even"] = (2, "Even"),
        ["odd"] = (2, "Odd"),
        ["low"] = (2, "1–18"),
        ["high"] = (2, "19–36"),
        ["dozen1"] = (3, "1st 12"),
        ["dozen2"] = (3, "2nd 12"),
        ["dozen3"] = (3, "3rd 12"),
        ["col1"] = (3, "Column 1"),
        ["col2"] = (3, "Column 2"),
        ["col3"] = (3, "Column 3"),
    };

    private sealed record PlacedBet(string Kind, int? Number, int Amount);

    private sealed class GameSession
    {
        public DateTime LastSeenUtc;
        public long Version;
        public string Status = "Place your bets and press Spin.";
        public List<PlacedBet> Bets = new();
        public int? LastNumber;
        public decimal LastPayout;
        public List<int> History = new();
    }

    private readonly object _sync = new();
    private readonly Random _rng = new();
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly Dictionary<int, GameSession> _sessions = new();

    public RouletteGameService(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    // ── Javne operacije ──────────────────────────────────────────────────────

    public RouletteStateDTO GetState(int playerId)
        => Mutate(playerId, (_, _, _) => { });

    public RouletteStateDTO PlaceBet(int playerId, string kind, int? number, int amount) => Mutate(playerId, (s, _, p) =>
    {
        if (!AllowedChips.Contains(amount))
        {
            s.Status = "Invalid chip amount.";
            return;
        }
        if (!IsValidKind(kind, number))
        {
            s.Status = "Invalid bet.";
            return;
        }
        if (s.Bets.Count >= MaxBetsPerSpin)
        {
            s.Status = $"At most {MaxBetsPerSpin} bets per spin.";
            return;
        }
        if (TotalBet(s) + amount > p.Balance)
        {
            s.Status = "Not enough balance for this bet.";
            return;
        }

        s.Bets.Add(new PlacedBet(kind, kind == "straight" ? number : null, amount));
        s.Status = $"Bet ${amount} on {BetLabel(s.Bets[^1])}. Total: ${TotalBet(s)}.";
    });

    public RouletteStateDTO ClearBets(int playerId) => Mutate(playerId, (s, _, _) =>
    {
        s.Bets.Clear();
        s.Status = "Bets cleared. Place your bets.";
    });

    public RouletteStateDTO Spin(int playerId) => Mutate(playerId, (s, db, p) =>
    {
        var total = TotalBet(s);
        if (total == 0)
        {
            s.Status = "Place at least one bet first.";
            return;
        }
        if (total > p.Balance)
        {
            // Saldo se mogao smanjiti nakon polaganja (npr. blackjack u drugom tabu)
            s.Status = "Balance no longer covers your bets. Clear and re-bet.";
            return;
        }

        p.Balance -= total;
        AddTransaction(db, p.Id, TransactionType.Bet, total);

        var result = _rng.Next(37); // 0–36
        decimal payout = s.Bets.Where(b => BetWins(b, result)).Sum(b => (decimal)b.Amount * PayoutFor(b));

        if (payout > 0)
        {
            p.Balance += payout;
            AddTransaction(db, p.Id, TransactionType.Win, payout);
        }

        s.LastNumber = result;
        s.LastPayout = payout;
        s.History.Insert(0, result);
        if (s.History.Count > 10) s.History.RemoveAt(s.History.Count - 1);
        s.Bets.Clear();
        s.Status = payout > 0
            ? $"{ResultLabel(result)} — you win ${payout:0.##}!"
            : $"{ResultLabel(result)} — no win this time.";
    });

    // ── Pravila ──────────────────────────────────────────────────────────────

    private static bool IsValidKind(string kind, int? number) =>
        kind == "straight" ? number is >= 0 and <= 36 : OutsideBets.ContainsKey(kind);

    private static int PayoutFor(PlacedBet bet) =>
        bet.Kind == "straight" ? 36 : OutsideBets[bet.Kind].Payout;

    private static bool BetWins(PlacedBet bet, int n) => bet.Kind switch
    {
        "straight" => n == bet.Number,
        "red" => RedNumbers.Contains(n),
        "black" => n != 0 && !RedNumbers.Contains(n),
        "even" => n != 0 && n % 2 == 0,
        "odd" => n % 2 == 1,
        "low" => n is >= 1 and <= 18,
        "high" => n is >= 19 and <= 36,
        "dozen1" => n is >= 1 and <= 12,
        "dozen2" => n is >= 13 and <= 24,
        "dozen3" => n is >= 25 and <= 36,
        "col1" => n != 0 && n % 3 == 1,
        "col2" => n != 0 && n % 3 == 2,
        "col3" => n != 0 && n % 3 == 0,
        _ => false
    };

    private static string BetLabel(PlacedBet bet) =>
        bet.Kind == "straight" ? $"Number {bet.Number}" : OutsideBets[bet.Kind].Label;

    private static string ColorOf(int n) =>
        n == 0 ? "green" : RedNumbers.Contains(n) ? "red" : "black";

    private static string ResultLabel(int n) =>
        n == 0 ? "0 (green)" : $"{n} ({ColorOf(n)})";

    private static int TotalBet(GameSession s) => s.Bets.Sum(b => b.Amount);

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

    // ── Sesije i pogled ──────────────────────────────────────────────────────

    private RouletteStateDTO Mutate(int playerId, Action<GameSession, CasinoDbContext, Player> action)
    {
        lock (_sync)
        {
            // Novac se skida tek na Spin, pa se ustajale sesije smiju samo baciti
            var now = DateTime.UtcNow;
            foreach (var stale in _sessions.Where(kv => now - kv.Value.LastSeenUtc > SessionTimeout).Select(kv => kv.Key).ToList())
            {
                _sessions.Remove(stale);
            }

            if (!_sessions.TryGetValue(playerId, out var session))
            {
                session = new GameSession();
                _sessions[playerId] = session;
            }
            session.LastSeenUtc = now;

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CasinoDbContext>();
            var player = db.Players.Find(playerId)
                ?? throw new InvalidOperationException($"Player {playerId} not found.");

            action(session, db, player);
            db.SaveChanges();
            session.Version++;
            return BuildView(session, player);
        }
    }

    private static RouletteStateDTO BuildView(GameSession s, Player p)
    {
        var total = TotalBet(s);
        return new RouletteStateDTO(
            Version: s.Version,
            Status: s.Status,
            PlayerName: $"{p.FirstName} {p.LastName}".Trim(),
            Balance: p.Balance,
            Bets: s.Bets.Select(b => new RouletteBetDTO(b.Kind, b.Number, b.Amount, BetLabel(b))).ToList(),
            TotalBet: total,
            LastNumber: s.LastNumber,
            LastColor: s.LastNumber is int n ? ColorOf(n) : null,
            LastPayout: s.LastPayout,
            History: new List<int>(s.History),
            CanBet: s.Bets.Count < MaxBetsPerSpin,
            CanSpin: total > 0 && total <= p.Balance,
            CanClear: s.Bets.Count > 0);
    }
}
