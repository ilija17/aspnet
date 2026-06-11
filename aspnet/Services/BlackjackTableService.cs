using aspnet.Models.DTO;

namespace aspnet.Services;

// Server-side blackjack stol: špil, dijeljenje, redoslijed poteza i isplate
// žive ovdje — klijent (wwwroot/kocka) je samo GUI koji zove API i crta stanje.
// Jedan zajednički stol s dva sjedala; klijent se identificira clientId-jem
// kojeg tab čuva u sessionStorageu, pa reload zadržava sjedalo.
public class BlackjackTableService
{
    private const int StartingBalance = 1000;
    private static readonly int[] AllowedBets = { 25, 50, 100, 200 };

    // Sjedalo čiji vlasnik ne polla duže od ovoga smatra se napuštenim
    private static readonly TimeSpan TurnTimeout = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan SeatTimeout = TimeSpan.FromSeconds(120);

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

    private sealed class Seat
    {
        public string? ClientId;
        public DateTime LastSeenUtc;
        public int Balance = StartingBalance;
        public int SelectedBet = 25;
        public int CurrentBet;
        public List<Card> Hand = new();
        public bool Stood;
        public bool Bust;
        public bool Blackjack;
        public int Wins;
        public int Losses;
        public int Pushes;
    }

    private readonly object _sync = new();
    private readonly Random _rng = new();

    private string _phase = "waiting";
    private string _status = "Join Seat 1 or Seat 2 to play.";
    private bool _soloMode;
    private bool _revealDealer;
    private int? _turnSeat;
    private long _version;
    private List<Card> _deck = new();
    private List<Card> _dealerHand = new();
    private readonly Dictionary<int, Seat> _seats = new() { [1] = new Seat(), [2] = new Seat() };
    private readonly Dictionary<int, string?> _lastRoundResults = new() { [1] = null, [2] = null };

    // ── Javne operacije (sve vraćaju personalizirani pogled na stol) ────────

    public BlackjackStateDTO GetState(string clientId)
        => Mutate(clientId, _ => { });

    public BlackjackStateDTO Join(string clientId, int seatNumber) => Mutate(clientId, _ =>
    {
        if (InActiveHand()) return;

        var target = _seats[seatNumber];
        if (_soloMode)
        {
            var otherOccupied = ActiveSeats().Any(s => s != seatNumber);
            if (otherOccupied)
            {
                _status = "Solo mode allows only one occupied seat. Leave or reset first.";
                return;
            }
        }

        if (target.ClientId != null && target.ClientId != clientId)
        {
            _status = $"Seat {seatNumber} is already taken.";
            return;
        }

        // Premještanje: oslobodi staro sjedalo
        var current = FindSeat(clientId);
        if (current is int old && old != seatNumber)
        {
            _seats[old].ClientId = null;
        }

        target.ClientId = clientId;
        target.LastSeenUtc = DateTime.UtcNow;
        _status = $"Player {seatNumber} joined. Select your bet and press Deal when ready.";
    });

    public BlackjackStateDTO Leave(string clientId) => Mutate(clientId, seat =>
    {
        if (seat is not int n) return;
        if (InActiveHand())
        {
            _status = "Cannot leave seat during an active hand.";
            return;
        }
        VacateSeat(n);
        _status = "Seat released.";
    });

    public BlackjackStateDTO ToggleSolo(string clientId) => Mutate(clientId, _ =>
    {
        if (InActiveHand()) return;
        if (!_soloMode && ActiveSeats().Count > 1)
        {
            _status = "Leave one seat first, then enable Solo Mode.";
            return;
        }
        _soloMode = !_soloMode;
        _status = _soloMode
            ? "Solo Mode enabled. One occupied seat can play."
            : "Solo Mode disabled. Two occupied seats are required.";
    });

    public BlackjackStateDTO Reset(string clientId) => Mutate(clientId, _ =>
    {
        if (InActiveHand()) return;
        _phase = "waiting";
        _turnSeat = null;
        _deck = new List<Card>();
        _dealerHand = new List<Card>();
        _revealDealer = false;
        _lastRoundResults[1] = null;
        _lastRoundResults[2] = null;
        _seats[1] = new Seat();
        _seats[2] = new Seat();
        _status = "Table reset. Join a seat to start.";
    });

    public BlackjackStateDTO SetBet(string clientId, int amount) => Mutate(clientId, seat =>
    {
        if (seat is not int n || InActiveHand()) return;
        if (!AllowedBets.Contains(amount))
        {
            _status = "Invalid bet.";
            return;
        }
        var player = _seats[n];
        if (amount > player.Balance)
        {
            _status = "Bet too high for your current balance.";
            return;
        }
        player.SelectedBet = amount;
        _status = $"Player {n} selected ${amount}.";
    });

    public BlackjackStateDTO Deal(string clientId) => Mutate(clientId, seat =>
    {
        if (seat is null || InActiveHand()) return;
        if (!SeatsReadyToDeal())
        {
            _status = _soloMode
                ? "Solo mode needs one occupied seat with a valid bet and balance."
                : "Two occupied seats with valid bets and balances are required.";
            return;
        }

        _deck = MakeShuffledDeck();
        _dealerHand = new List<Card> { DrawCard(), DrawCard() };
        _revealDealer = false;
        _phase = "player-turns";
        _lastRoundResults[1] = null;
        _lastRoundResults[2] = null;

        foreach (var n in ActiveSeats())
        {
            var player = _seats[n];
            player.Balance -= player.SelectedBet;
            player.CurrentBet = player.SelectedBet;
            player.Hand = new List<Card> { DrawCard(), DrawCard() };
            player.Bust = false;
            player.Stood = false;
            player.Blackjack = IsBlackjack(player.Hand);
            if (player.Blackjack) player.Stood = true;
        }

        var firstTurn = NextTurnSeat(null);
        _turnSeat = firstTurn;
        if (firstTurn is null)
        {
            RunDealer();
            return;
        }
        _status = $"Round started. Player {firstTurn}'s turn.";
    });

    public BlackjackStateDTO Hit(string clientId) => Mutate(clientId, seat =>
    {
        if (!IsTurnOf(seat, out var n)) return;
        var player = _seats[n];
        player.Hand.Add(DrawCard());
        if (ScoreHand(player.Hand) > 21)
        {
            player.Bust = true;
            player.Stood = true;
            _status = $"Player {n} busts.";
            AdvanceTurn(n);
            return;
        }
        _status = $"Player {n} hits.";
    });

    public BlackjackStateDTO Stand(string clientId) => Mutate(clientId, seat =>
    {
        if (!IsTurnOf(seat, out var n)) return;
        _seats[n].Stood = true;
        _status = $"Player {n} stands.";
        AdvanceTurn(n);
    });

    public BlackjackStateDTO Double(string clientId) => Mutate(clientId, seat =>
    {
        if (!IsTurnOf(seat, out var n)) return;
        var player = _seats[n];
        if (player.Hand.Count != 2 || player.Balance < player.CurrentBet)
        {
            _status = "Double Down unavailable.";
            return;
        }
        player.Balance -= player.CurrentBet;
        player.CurrentBet *= 2;
        player.Hand.Add(DrawCard());
        player.Stood = true;
        _status = ScoreHand(player.Hand) > 21
            ? $"Player {n} busts after Double Down."
            : $"Player {n} doubles to ${player.CurrentBet}.";
        if (ScoreHand(player.Hand) > 21) player.Bust = true;
        AdvanceTurn(n);
    });

    // ── Tijek igre ───────────────────────────────────────────────────────────

    private bool InActiveHand() => _phase is "player-turns" or "dealer-turn";

    private bool IsTurnOf(int? seat, out int n)
    {
        n = seat ?? 0;
        return seat is int s && _phase == "player-turns" && _turnSeat == s;
    }

    private List<int> ActiveSeats() => _seats.Where(kv => kv.Value.ClientId != null).Select(kv => kv.Key).ToList();

    private bool SeatsReadyToDeal()
    {
        var seats = ActiveSeats();
        var required = _soloMode ? 1 : 2;
        if (seats.Count < required) return false;
        return seats.All(n =>
        {
            var p = _seats[n];
            return p.Balance >= p.SelectedBet && p.SelectedBet > 0;
        });
    }

    private int? NextTurnSeat(int? currentSeat)
    {
        var order = new[] { 1, 2 };
        var start = currentSeat is int c ? Array.IndexOf(order, c) + 1 : 0;
        for (var offset = 0; offset < order.Length; offset++)
        {
            var n = order[(start + offset) % order.Length];
            var p = _seats[n];
            if (p.ClientId != null && p.Hand.Count > 0 && !p.Stood && !p.Bust && !p.Blackjack)
            {
                return n;
            }
        }
        return null;
    }

    private void AdvanceTurn(int seatThatMoved)
    {
        var next = NextTurnSeat(seatThatMoved);
        if (next is int n)
        {
            _turnSeat = n;
            _phase = "player-turns";
            _status = $"Player {n}'s turn.";
            return;
        }
        RunDealer();
    }

    private void RunDealer()
    {
        _phase = "dealer-turn";
        _revealDealer = true;
        while (ScoreHand(_dealerHand) < 17)
        {
            _dealerHand.Add(DrawCard());
        }
        ResolveRound();
    }

    private void ResolveRound()
    {
        var dealerTotal = ScoreHand(_dealerHand);
        var dealerBlackjack = IsBlackjack(_dealerHand);
        var summaries = new List<string>();
        _lastRoundResults[1] = null;
        _lastRoundResults[2] = null;

        foreach (var n in ActiveSeats())
        {
            var player = _seats[n];
            if (player.CurrentBet <= 0) continue;

            var playerTotal = ScoreHand(player.Hand);
            string result;
            var payout = 0;

            if (player.Bust)
            {
                result = "loss";
            }
            else if (player.Blackjack && !dealerBlackjack)
            {
                result = "win";
                payout = (int)Math.Floor(player.CurrentBet * 2.5);
            }
            else if (dealerTotal > 21 || playerTotal > dealerTotal)
            {
                result = "win";
                payout = player.CurrentBet * 2;
            }
            else if (playerTotal == dealerTotal)
            {
                result = "push";
                payout = player.CurrentBet;
            }
            else
            {
                result = "loss";
            }

            switch (result)
            {
                case "win":
                    player.Wins++;
                    player.Balance += payout;
                    summaries.Add($"P{n} wins");
                    break;
                case "push":
                    player.Pushes++;
                    player.Balance += payout;
                    summaries.Add($"P{n} pushes");
                    break;
                default:
                    player.Losses++;
                    summaries.Add($"P{n} loses");
                    break;
            }

            _lastRoundResults[n] = result;
            player.CurrentBet = 0;
            player.Stood = true;
        }

        _phase = "round-over";
        _turnSeat = null;
        _revealDealer = true;
        _status = $"Round over: {string.Join(", ", summaries)}. Press Deal for next hand.";
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

    private Card DrawCard()
    {
        if (_deck.Count == 0)
        {
            _deck = MakeShuffledDeck();
        }
        var card = _deck[^1];
        _deck.RemoveAt(_deck.Count - 1);
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

    // ── Sjedala, timeouti, pogled ────────────────────────────────────────────

    private int? FindSeat(string clientId) =>
        _seats.Where(kv => kv.Value.ClientId == clientId).Select(kv => (int?)kv.Key).FirstOrDefault();

    private void VacateSeat(int n)
    {
        var seat = _seats[n];
        seat.ClientId = null;
        seat.Hand = new List<Card>();
        seat.CurrentBet = 0;
    }

    // Klijenti koji nestanu bez Leave (zatvoren tab, izgubljena mreža) ne smiju
    // trajno blokirati stol: na potezu ih auto-stand, izvan ruke oslobodi sjedalo.
    private void SweepInactive()
    {
        var now = DateTime.UtcNow;

        if (_phase == "player-turns" && _turnSeat is int t)
        {
            var seat = _seats[t];
            if (seat.ClientId != null && now - seat.LastSeenUtc > TurnTimeout)
            {
                seat.Stood = true;
                _status = $"Player {t} timed out and stands.";
                AdvanceTurn(t);
            }
        }

        if (!InActiveHand())
        {
            foreach (var n in ActiveSeats())
            {
                if (now - _seats[n].LastSeenUtc > SeatTimeout)
                {
                    VacateSeat(n);
                    _status = $"Seat {n} released (inactive).";
                }
            }
        }
    }

    private BlackjackStateDTO Mutate(string clientId, Action<int?> action)
    {
        lock (_sync)
        {
            var seat = FindSeat(clientId);
            if (seat is int n)
            {
                _seats[n].LastSeenUtc = DateTime.UtcNow;
            }
            SweepInactive();
            action(FindSeat(clientId));
            _version++;
            return BuildView(clientId);
        }
    }

    private BlackjackStateDTO BuildView(string clientId)
    {
        var yourSeat = FindSeat(clientId);
        var localPlayer = yourSeat is int ys ? _seats[ys] : null;
        var inActiveHand = InActiveHand();
        var someoneSeated = ActiveSeats().Count > 0;
        var isMyTurn = localPlayer != null && _phase == "player-turns" && _turnSeat == yourSeat;

        var dealerHand = _dealerHand
            .Select((card, index) => !_revealDealer && index == 1
                ? new BlackjackCardDTO(null, null, null, true)
                : new BlackjackCardDTO(card.Rank, card.Suit, card.Color, false))
            .ToList();

        var players = _seats.ToDictionary(
            kv => kv.Key,
            kv => new BlackjackSeatDTO(
                Occupied: kv.Value.ClientId != null,
                IsYou: kv.Value.ClientId == clientId,
                Balance: kv.Value.Balance,
                SelectedBet: kv.Value.SelectedBet,
                CurrentBet: kv.Value.CurrentBet,
                Hand: kv.Value.Hand.Select(c => new BlackjackCardDTO(c.Rank, c.Suit, c.Color, false)).ToList(),
                Total: ScoreHand(kv.Value.Hand),
                Stood: kv.Value.Stood,
                Bust: kv.Value.Bust,
                Blackjack: kv.Value.Blackjack,
                Wins: kv.Value.Wins,
                Losses: kv.Value.Losses,
                Pushes: kv.Value.Pushes));

        bool CanJoin(int n)
        {
            var takenByOther = _seats[n].ClientId != null && _seats[n].ClientId != clientId;
            return !takenByOther
                   && yourSeat != n
                   && !(_soloMode && someoneSeated && _seats[n].ClientId == null)
                   && !inActiveHand;
        }

        return new BlackjackStateDTO(
            Version: _version,
            Phase: _phase,
            Status: _status,
            SoloMode: _soloMode,
            RevealDealer: _revealDealer,
            TurnSeat: _turnSeat,
            YourSeat: yourSeat,
            DealerHand: dealerHand,
            DealerTotal: _revealDealer ? ScoreHand(_dealerHand) : null,
            LastRoundResults: new Dictionary<int, string?>(_lastRoundResults),
            Players: players,
            CanJoin1: CanJoin(1),
            CanJoin2: CanJoin(2),
            CanLeave: yourSeat != null && !inActiveHand,
            CanToggleSolo: !inActiveHand,
            CanReset: !inActiveHand,
            CanSetBet: localPlayer != null && !inActiveHand,
            CanDeal: localPlayer != null && SeatsReadyToDeal() && !inActiveHand,
            CanHit: isMyTurn,
            CanStand: isMyTurn,
            CanDouble: isMyTurn && localPlayer!.Hand.Count == 2 && localPlayer.Balance >= localPlayer.CurrentBet);
    }
}
