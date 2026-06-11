using System;
using System.Collections.Generic;
using System.Linq;
using aspnet.Data;
using aspnet.Models;
using aspnet.Models.DTO;

namespace aspnet.Services;

// Runda se simulira i razrješava u jednom pozivu (start); klijent dobiva
// cijelu snimljenu putanju i sam je reproducira — nema server-side playbacka.
public class ThreeBodyGameService
{
    public static readonly int[] AllowedBets = { 25, 50, 100, 200 };
    private static readonly TimeSpan SessionTimeout = TimeSpan.FromMinutes(30);
    private const decimal PayoutMultiplier = 3m;

    // G ×4 + početne brzine ×2 = iste putanje, ali 2× brže odigrane.
    private const double G = 640.0;
    private const double Softening = 10.0;
    private const double EjectionRadius = 1200.0;
    private const double CollisionFactor = 0.8;
    private const double PhysicsDt = 0.04;
    private const int MaxPhysicsSteps = 6000;
    private const int RecordEvery = 2;

    private static readonly (string Name, string Color, double Mass, double Radius)[] PlanetTemplates =
    {
        ("A", "#ff6b6b", 400, 32),
        ("B", "#4ecdc4", 250, 24),
        ("C", "#ffe66d", 150, 28),
    };

    private sealed class PlanetState
    {
        public string Name = "";
        public double Mass;
        public double Radius;
        public double X, Y, Vx, Vy;
        public bool Alive = true;
    }

    private sealed class GameSession
    {
        public DateTime LastSeenUtc;
        public long Version;
        public string Status = "Bet on which planet will survive the longest.";
        public int SelectedBet = 25;
        public string? BetOnPlanet;
        public string? LastResult;
        public string? LastWinner;
        public decimal LastPayout;
        public int Wins;
        public int Losses;
    }

    private readonly object _sync = new();
    private readonly Random _rng = new();
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly Dictionary<int, GameSession> _sessions = new();

    public ThreeBodyGameService(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    public ThreeBodyStateDTO GetState(int playerId)
        => Mutate(playerId, (s, _, _) => { });

    public ThreeBodyStateDTO SetBet(int playerId, int amount, string planet)
        => Mutate(playerId, (s, _, p) =>
        {
            if (amount > p.Balance)
            {
                s.Status = "Bet too high for your balance.";
                return;
            }
            s.SelectedBet = amount;
            s.BetOnPlanet = planet;
            s.Status = $"Bet ${amount} on planet {planet}. Press Start to begin.";
        });

    public ThreeBodyStateDTO Start(int playerId)
    {
        ThreeBodyRoundDTO? round = null;

        var state = Mutate(playerId, (s, db, p) =>
        {
            if (s.BetOnPlanet is null)
            {
                s.Status = "Select a planet first.";
                return;
            }
            if (s.SelectedBet > p.Balance)
            {
                s.Status = "Insufficient balance for this bet.";
                return;
            }

            p.Balance -= s.SelectedBet;
            AddTransaction(db, p.Id, TransactionType.Bet, s.SelectedBet);

            var (frames, eliminations, winner) = ComputeTrajectory();

            decimal payout = 0;
            string result;
            if (s.BetOnPlanet == winner)
            {
                payout = s.SelectedBet * PayoutMultiplier;
                p.Balance += payout;
                AddTransaction(db, p.Id, TransactionType.Win, payout);
                s.Wins++;
                result = "win";
                s.Status = $"Planet {winner} survived the longest! You win ${payout:0.##}!";
            }
            else
            {
                s.Losses++;
                result = "loss";
                s.Status = winner is not null
                    ? $"Planet {winner} survived the longest. You bet on {s.BetOnPlanet}. You lose."
                    : "All planets destroyed! You lose.";
            }

            round = new ThreeBodyRoundDTO(
                Frames: frames,
                Eliminations: eliminations,
                WinnerPlanet: winner,
                BetPlanet: s.BetOnPlanet,
                BetAmount: s.SelectedBet,
                Payout: payout,
                Result: result);

            s.LastResult = result;
            s.LastWinner = winner;
            s.LastPayout = payout;
            s.BetOnPlanet = null;
        });

        return state with { Round = round };
    }

    private (double[][] Frames, List<ThreeBodyEliminationDTO> Eliminations, string? Winner) ComputeTrajectory()
    {
        var planets = PlanetTemplates.Select(t => new PlanetState
        {
            Name = t.Name,
            Mass = t.Mass,
            Radius = t.Radius,
            Alive = true
        }).ToArray();

        InitializePositions(planets);

        var frames = new List<double[]>();
        var eliminations = new List<ThreeBodyEliminationDTO>();
        var eliminated = new HashSet<string>();

        void Eliminate(PlanetState planet, int frameIdx)
        {
            planet.Alive = false;
            if (eliminated.Add(planet.Name))
                eliminations.Add(new ThreeBodyEliminationDTO(frameIdx, planet.Name));
        }

        double[] Snapshot() => planets
            .SelectMany(p => new[] { Math.Round(p.X, 1), Math.Round(p.Y, 1) })
            .ToArray();

        for (var step = 0; step < MaxPhysicsSteps; step++)
        {
            if (step % RecordEvery == 0)
                frames.Add(Snapshot());
            var frameIdx = frames.Count - 1;

            StepRK4(planets);

            foreach (var planet in planets)
            {
                if (planet.Alive && Math.Sqrt(planet.X * planet.X + planet.Y * planet.Y) > EjectionRadius)
                    Eliminate(planet, frameIdx);
            }

            for (var i = 0; i < planets.Length; i++)
            {
                if (!planets[i].Alive) continue;
                for (var j = i + 1; j < planets.Length; j++)
                {
                    if (!planets[j].Alive) continue;
                    var dx = planets[i].X - planets[j].X;
                    var dy = planets[i].Y - planets[j].Y;
                    var dist = Math.Sqrt(dx * dx + dy * dy);
                    if (dist < (planets[i].Radius + planets[j].Radius) * CollisionFactor)
                    {
                        Eliminate(planets[i], frameIdx);
                        Eliminate(planets[j], frameIdx);
                    }
                }
            }

            if (planets.Count(p => p.Alive) <= 1)
            {
                frames.Add(Snapshot());
                break;
            }
        }

        var winner = planets.Count(p => p.Alive) == 1
            ? planets.First(p => p.Alive).Name
            : null;

        return (frames.ToArray(), eliminations, winner);
    }

    private void InitializePositions(PlanetState[] planets)
    {
        var configs = new (double angle, double dist, double speed)[][] {
            new[] { (0.0,  220.0, 20.0), (2.3,  240.0, -18.0),  (4.5,  200.0, 24.0) },
            new[] { (1.0,  230.0, 16.0),  (3.1,  190.0, -24.0), (5.2,  250.0, -14.0) },
            new[] { (0.5,  200.0, -28.0), (2.7,  260.0, 12.0),  (4.8,  220.0, -22.0) },
            new[] { (1.8,  240.0, 18.0),  (3.9,  210.0, -20.0), (0.2,  230.0, 26.0) },
            new[] { (0.3,  250.0, -12.0), (2.5,  200.0, 28.0),  (5.0,  240.0, -16.0) },
            new[] { (1.4,  210.0, 22.0), (3.6,  240.0, -14.0),  (5.7,  190.0, 16.0) },
        };

        var config = configs[_rng.Next(configs.Length)];

        var cx = _rng.NextDouble() * 80 - 40;
        var cy = _rng.NextDouble() * 80 - 40;

        for (var i = 0; i < planets.Length; i++)
        {
            var angle = config[i].angle + (_rng.NextDouble() - 0.5) * 0.5;
            var dist = config[i].dist + (_rng.NextDouble() - 0.5) * 50;
            var speed = config[i].speed + (_rng.NextDouble() - 0.5) * 6.0;
            var perpAngle = angle + Math.PI / 2;

            planets[i].X = cx + Math.Cos(angle) * dist;
            planets[i].Y = cy + Math.Sin(angle) * dist;
            planets[i].Vx = Math.Cos(perpAngle) * speed;
            planets[i].Vy = Math.Sin(perpAngle) * speed;
        }
    }

    private static void StepRK4(PlanetState[] planets)
    {
        var n = planets.Length;
        var state = new double[n * 4];
        for (var i = 0; i < n; i++)
        {
            state[i * 4 + 0] = planets[i].X;
            state[i * 4 + 1] = planets[i].Y;
            state[i * 4 + 2] = planets[i].Vx;
            state[i * 4 + 3] = planets[i].Vy;
        }

        double[] Deriv(double[] s)
        {
            var d = new double[n * 4];
            for (var i = 0; i < n; i++)
            {
                var idx = i * 4;
                d[idx + 0] = s[idx + 2];
                d[idx + 1] = s[idx + 3];
                double ax = 0, ay = 0;
                for (var j = 0; j < n; j++)
                {
                    if (i == j || !planets[j].Alive) continue;
                    var jdx = j * 4;
                    var dx = s[jdx + 0] - s[idx + 0];
                    var dy = s[jdx + 1] - s[idx + 1];
                    var rSq = dx * dx + dy * dy + Softening * Softening;
                    var invR3 = 1.0 / (Math.Sqrt(rSq) * rSq);
                    var force = G * planets[j].Mass * invR3;
                    ax += force * dx;
                    ay += force * dy;
                }
                d[idx + 2] = ax;
                d[idx + 3] = ay;
            }
            return d;
        }

        var k1 = Deriv(state);
        var s2 = new double[n * 4]; for (var i = 0; i < n * 4; i++) s2[i] = state[i] + 0.5 * PhysicsDt * k1[i];
        var k2 = Deriv(s2);
        var s3 = new double[n * 4]; for (var i = 0; i < n * 4; i++) s3[i] = state[i] + 0.5 * PhysicsDt * k2[i];
        var k3 = Deriv(s3);
        var s4 = new double[n * 4]; for (var i = 0; i < n * 4; i++) s4[i] = state[i] + PhysicsDt * k3[i];
        var k4 = Deriv(s4);

        for (var i = 0; i < n; i++)
        {
            var idx = i * 4;
            planets[i].X += (PhysicsDt / 6.0) * (k1[idx + 0] + 2 * k2[idx + 0] + 2 * k3[idx + 0] + k4[idx + 0]);
            planets[i].Y += (PhysicsDt / 6.0) * (k1[idx + 1] + 2 * k2[idx + 1] + 2 * k3[idx + 1] + k4[idx + 1]);
            planets[i].Vx += (PhysicsDt / 6.0) * (k1[idx + 2] + 2 * k2[idx + 2] + 2 * k3[idx + 2] + k4[idx + 2]);
            planets[i].Vy += (PhysicsDt / 6.0) * (k1[idx + 3] + 2 * k2[idx + 3] + 2 * k3[idx + 3] + k4[idx + 3]);
        }
    }

    private ThreeBodyStateDTO Mutate(int playerId, Action<GameSession, CasinoDbContext, Player> action)
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

            // Igrača uvijek učitavamo u vlastiti kontekst da SaveChanges
            // stvarno zapiše promjenu salda.
            var player = db.Players.Find(playerId)
                ?? throw new InvalidOperationException($"Player {playerId} not found.");

            action(session, db, player);

            db.SaveChanges();
            session.Version++;
            return BuildView(session, player);
        }
    }

    private static ThreeBodyStateDTO BuildView(GameSession s, Player p)
        => new(
            Version: s.Version,
            Status: s.Status,
            PlayerName: $"{p.FirstName} {p.LastName}".Trim(),
            Balance: p.Balance,
            SelectedBet: s.SelectedBet,
            BetOnPlanet: s.BetOnPlanet,
            Planets: PlanetTemplates
                .Select(t => new ThreeBodyPlanetDTO(t.Name, t.Color, t.Mass, t.Radius))
                .ToList(),
            LastResult: s.LastResult,
            LastWinner: s.LastWinner,
            LastPayout: s.LastPayout,
            Wins: s.Wins,
            Losses: s.Losses,
            CanStart: s.BetOnPlanet is not null && s.SelectedBet <= p.Balance,
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
