using aspnet.Data;
using aspnet.Models;
using aspnet.Models.DTO;

namespace aspnet.Services;

// Server-side singleplayer blackjack: špil, dijeljenje i isplate žive ovdje —
// klijent (wwwroot/kocka) je samo GUI koji zove API i crta stanje.
// Ruka je u memoriji po igraču, ali novac je stvarni Player.Balance:
// ulozi i isplate idu kroz bazu kao Bet/Win transakcije.
public class BlackjackGameService
{
    private static readonly int[] AllowedBets = { 25, 50, 100, 200 };

    // Napuštene sesije (zatvoren tab) ne smiju zauvijek držati memoriju;
    // ruka u tijeku se pri isteku tretira kao stand da se ulog ne izgubi
    private static readonly TimeSpan SessionTimeout = TimeSpan.FromMinutes(30);

    private static readonly (string Suit, string Color)[] Suits =
    {
        ("♥", "red"), ("♦", "red"), ("♣", "black"), ("♠", "black")
    };

    private static readonly (string Rank, int Value)[] Ranks =
    {
        ("A", 11), ("K", 10), ("Q", 10), ("J", 10), ("10", 10),
        ("9", 9), ("8", 8), ("7", 7), ("6", 6), ("5", 5), ("4", 4), ("3", 3), ("2", 2)
    };

    private sealed record Card(string Rank, string Suit, string Color, int Value);

    private sealed class GameSession
    {
        public DateTime LastSeenUtc;
        public long Version;
        public string Phase = "betting";
        public string Status = "Select your bet and press Deal.";
        public bool RevealDealer;
        public int SelectedBet = 25;
        public int CurrentBet;
        public List<Card> Deck = new();
        public List<Card> DealerHand = new();
        public List<Card> Hand = new();
        public bool Stood;
        public bool Bust;
        public bool Blackjack;
        public string? LastRoundResult;
        public int Wins;
        public int Losses;
        public int Pushes;
    }

    private readonly object _sync = new();
    private readonly Random _rng = new();
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly Dictionary<int, GameSession> _sessions = new();

    public BlackjackGameService(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    // ── Javne operacije ──────────────────────────────────────────────────────

    public BlackjackStateDTO GetState(int playerId)
        => Mutate(playerId, (_, _, _) => { });

    public BlackjackStateDTO SetBet(int playerId, int amount) => Mutate(playerId, (s, _, p) =>
    {
        if (s.Phase == "player-turn") return;
        if (!AllowedBets.Contains(amount))
        {
            s.Status = "Invalid bet.";
            return;
        }
        if (amount > p.Balance)
        {
            s.Status = "Bet too high for your current balance.";
            return;
        }
        s.SelectedBet = amount;
        s.Status = $"Bet set to ${amount}.";
    });

    public BlackjackStateDTO Deal(int playerId) => Mutate(playerId, (s, db, p) =>
    {
        if (s.Phase == "player-turn") return;
        if (p.Balance < s.SelectedBet)
        {
            s.Status = "Insufficient balance for this bet.";
            return;
        }

        p.Balance -= s.SelectedBet;
        AddTransaction(db, p.Id, TransactionType.Bet, s.SelectedBet);

        s.Deck = MakeShuffledDeck();
        s.DealerHand = new List<Card> { DrawCard(s), DrawCard(s) };
        s.Hand = new List<Card> { DrawCard(s), DrawCard(s) };
        s.CurrentBet = s.SelectedBet;
        s.RevealDealer = false;
        s.Stood = false;
        s.Bust = false;
        s.LastRoundResult = null;
        s.Blackjack = IsBlackjack(s.Hand);
        s.Phase = "player-turn";

        if (s.Blackjack)
        {
            s.Stood = true;
            RunDealer(s, db, p);
            return;
        }
        s.Status = "Your turn: Hit, Stand or Double Down.";
    });

    public BlackjackStateDTO Hit(int playerId) => Mutate(playerId, (s, db, p) =>
    {
        if (!CanAct(s)) return;
        s.Hand.Add(DrawCard(s));
        if (ScoreHand(s.Hand) > 21)
        {
            s.Bust = true;
            s.Stood = true;
            RunDealer(s, db, p);
            return;
        }
        s.Status = $"You hit ({ScoreHand(s.Hand)}).";
    });

    public BlackjackStateDTO Stand(int playerId) => Mutate(playerId, (s, db, p) =>
    {
        if (!CanAct(s)) return;
        s.Stood = true;
        RunDealer(s, db, p);
    });

    public BlackjackStateDTO Double(int playerId) => Mutate(playerId, (s, db, p) =>
    {
        if (!CanAct(s)) return;
        if (s.Hand.Count != 2 || p.Balance < s.CurrentBet)
        {
            s.Status = "Double Down unavailable.";
            return;
        }
        p.Balance -= s.CurrentBet;
        AddTransaction(db, p.Id, TransactionType.Bet, s.CurrentBet);
        s.CurrentBet *= 2;
        s.Hand.Add(DrawCard(s));
        s.Stood = true;
        if (ScoreHand(s.Hand) > 21) s.Bust = true;
        RunDealer(s, db, p);
    });

    // ── Tijek igre ───────────────────────────────────────────────────────────

    private static bool CanAct(GameSession s) => s.Phase == "player-turn" && !s.Stood && !s.Bust;

    private void RunDealer(GameSession s, CasinoDbContext db, Player p)
    {
        s.RevealDealer = true;
        // Bustanom igraču dealer ne vuče — ulog je ionako izgubljen
        if (!s.Bust)
        {
            while (ScoreHand(s.DealerHand) < 17)
            {
                s.DealerHand.Add(DrawCard(s));
            }
        }
        ResolveRound(s, db, p);
    }

    private static void ResolveRound(GameSession s, CasinoDbContext db, Player p)
    {
        var dealerTotal = ScoreHand(s.DealerHand);
        var dealerBlackjack = IsBlackjack(s.DealerHand);
        var playerTotal = ScoreHand(s.Hand);

        string result;
        decimal payout = 0;

        if (s.Bust)
        {
            result = "loss";
        }
        else if (s.Blackjack && !dealerBlackjack)
        {
            result = "win";
            payout = s.CurrentBet * 2.5m;
        }
        else if (dealerTotal > 21 || playerTotal > dealerTotal)
        {
            result = "win";
            payout = s.CurrentBet * 2;
        }
        else if (playerTotal == dealerTotal)
        {
            result = "push";
            payout = s.CurrentBet;
        }
        else
        {
            result = "loss";
        }

        if (payout > 0)
        {
            p.Balance += payout;
            AddTransaction(db, p.Id, TransactionType.Win, payout);
        }

        switch (result)
        {
            case "win":
                s.Wins++;
                s.Status = s.Blackjack ? $"Blackjack! You win ${payout}." : $"You win ${payout}.";
                break;
            case "push":
                s.Pushes++;
                s.Status = "Push. Your bet is returned.";
                break;
            default:
                s.Losses++;
                s.Status = s.Bust ? "Bust. You lose." : "Dealer wins.";
                break;
        }

        s.LastRoundResult = result;
        s.CurrentBet = 0;
        s.Phase = "round-over";
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

    // ── Špil i bodovanje ─────────────────────────────────────────────────────

    private List<Card> MakeShuffledDeck()
    {
        var deck = new List<Card>();
        foreach (var (suit, color) in Suits)
        {
            foreach (var (rank, value) in Ranks)
            {
                deck.Add(new Card(rank, suit, color, value));
            }
        }
        for (var i = deck.Count - 1; i > 0; i--)
        {
            var j = _rng.Next(i + 1);
            (deck[i], deck[j]) = (deck[j], deck[i]);
        }
        return deck;
    }

    private Card DrawCard(GameSession s)
    {
        if (s.Deck.Count == 0)
        {
            s.Deck = MakeShuffledDeck();
        }
        var card = s.Deck[^1];
        s.Deck.RemoveAt(s.Deck.Count - 1);
        return card;
    }

    private static int ScoreHand(List<Card> hand)
    {
        var total = hand.Sum(c => c.Value);
        var aces = hand.Count(c => c.Rank == "A");
        while (total > 21 && aces > 0)
        {
            total -= 10;
            aces--;
        }
        return total;
    }

    private static bool IsBlackjack(List<Card> hand) => hand.Count == 2 && ScoreHand(hand) == 21;

    // ── Sesije i pogled ──────────────────────────────────────────────────────

    private void SweepStale(CasinoDbContext db)
    {
        var now = DateTime.UtcNow;
        foreach (var (playerId, session) in _sessions.Where(kv => now - kv.Value.LastSeenUtc > SessionTimeout).ToList())
        {
            // Ruka u tijeku se zatvara kao stand da igrač ne izgubi ulog bez razloga
            if (session.Phase == "player-turn")
            {
                var player = db.Players.Find(playerId);
                if (player is not null)
                {
                    session.Stood = true;
                    RunDealer(session, db, player);
                }
            }
            _sessions.Remove(playerId);
        }
    }

    private BlackjackStateDTO Mutate(int playerId, Action<GameSession, CasinoDbContext, Player> action)
    {
        lock (_sync)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CasinoDbContext>();

            SweepStale(db);

            if (!_sessions.TryGetValue(playerId, out var session))
            {
                session = new GameSession();
                _sessions[playerId] = session;
            }
            session.LastSeenUtc = DateTime.UtcNow;

            var player = db.Players.Find(playerId)
                ?? throw new InvalidOperationException($"Player {playerId} not found.");

            action(session, db, player);
            db.SaveChanges();
            session.Version++;
            return BuildView(session, player);
        }
    }

    private static BlackjackStateDTO BuildView(GameSession s, Player p)
    {
        var dealerHand = s.DealerHand
            .Select((card, index) => !s.RevealDealer && index == 1
                ? new BlackjackCardDTO(null, null, null, true)
                : new BlackjackCardDTO(card.Rank, card.Suit, card.Color, false))
            .ToList();

        var inHand = s.Phase == "player-turn";
        var canAct = CanAct(s);

        return new BlackjackStateDTO(
            Version: s.Version,
            Phase: s.Phase,
            Status: s.Status,
            PlayerName: $"{p.FirstName} {p.LastName}".Trim(),
            Balance: p.Balance,
            SelectedBet: s.SelectedBet,
            CurrentBet: s.CurrentBet,
            RevealDealer: s.RevealDealer,
            DealerHand: dealerHand,
            DealerTotal: s.RevealDealer && s.DealerHand.Count > 0 ? ScoreHand(s.DealerHand) : null,
            Hand: s.Hand.Select(c => new BlackjackCardDTO(c.Rank, c.Suit, c.Color, false)).ToList(),
            Total: ScoreHand(s.Hand),
            Bust: s.Bust,
            Blackjack: s.Blackjack,
            LastRoundResult: s.LastRoundResult,
            Wins: s.Wins,
            Losses: s.Losses,
            Pushes: s.Pushes,
            CanSetBet: !inHand,
            CanDeal: !inHand && p.Balance >= s.SelectedBet,
            CanHit: canAct,
            CanStand: canAct,
            CanDouble: canAct && s.Hand.Count == 2 && p.Balance >= s.CurrentBet);
    }
}
