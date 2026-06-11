using System;
using System.Collections.Generic;
using System.Linq;
using aspnet.Data;
using aspnet.Models;
using aspnet.Models.DTO;

namespace aspnet.Services;

public class ThreeBodyGameService
{
    private static readonly int[] AllowedBets = { 25, 50, 100, 200 };
    private static readonly TimeSpan SessionTimeout = TimeSpan.FromMinutes(30);

    private const double G = 80.0;
    private const double Softening = 10.0;
    private const double EjectionRadius = 1200.0;
    private const double CollisionFactor = 0.8;
    private const double PhysicsDt = 0.05;
    private const int MaxPhysicsSteps = 5000;
    private const int RecordEvery = 2;
    private const double PlaybackFps = 45.0;

    private static readonly (string Name, string Color, double Mass, double Radius)[] PlanetTemplates =
    {
        ("A", "#ff6b6b", 400, 32),
        ("B", "#4ecdc4", 250, 24),
        ("C", "#ffe66d", 150, 28),
    };

    private sealed class PlanetState
    {
        public string Name = "";
        public string Color = "";
        public double Mass;
        public double Radius;
        public double X, Y, Vx, Vy;
        public bool Alive = true;
    }

    private sealed class Frame
    {
        public PlanetData[] Planets = Array.Empty<PlanetData>();
    }

    private sealed class GameSession
    {
        public DateTime LastSeenUtc;
        public long Version;
        public string Phase = "betting";
        public string Status = "Bet on which planet will survive the longest.";
        public int SelectedBet = 25;
        public string? BetOnPlanet;
        public Frame[]? Trajectory;
        public DateTime SimulationStartedAt;
        public string[] EliminatedOrder = Array.Empty<string>();
        public string? WinnerPlanet;
        public string? LastRoundResult;
        public int Wins;
        public int Losses;
    }

    private readonly object _sync = new();
    private readonly Random _rng = new();
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly Dictionary<int, GameSession> _sessions = new();

    public ThreeBodyGameService(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    public ThreeBodyStateDTO GetState(int playerId, Player? preloadedPlayer = null)
        => Mutate(playerId, (_, _, _) => { }, preloadedPlayer);

    public ThreeBodyStateDTO SetBet(int playerId, int amount, string planet, Player? preloadedPlayer = null)
        => Mutate(playerId, (s, _, p) =>
        {
            if (s.Phase != "betting") return;
            if (amount > p.Balance)
            {
                s.Status = "Bet too high for your balance.";
                return;
            }
            s.SelectedBet = amount;
            s.BetOnPlanet = planet;
            s.Status = $"Bet ${amount} on planet {planet}. Press Start to begin.";
        }, preloadedPlayer);

    public ThreeBodyStateDTO Start(int playerId, Player? preloadedPlayer = null)
        => Mutate(playerId, (s, db, p) =>
        {
            if (s.Phase != "betting") return;
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

            var result = ComputeTrajectory();
            s.Trajectory = result.Frames;
            s.EliminatedOrder = result.EliminatedOrder;
            s.WinnerPlanet = result.WinnerPlanet;
            s.SimulationStartedAt = DateTime.UtcNow;
            s.Phase = "simulating";
            s.Status = "Watch the chaos unfold!";
        }, preloadedPlayer);

    public ThreeBodyStateDTO SkipToEnd(int playerId, Player? preloadedPlayer = null)
        => Mutate(playerId, (s, db, p) =>
        {
            if (s.Phase != "simulating" || s.Trajectory is null) return;
            s.SimulationStartedAt = DateTime.MinValue;
            ResolveRound(s, db, p);
        }, preloadedPlayer);

    public ThreeBodyStateDTO Reset(int playerId, Player? preloadedPlayer = null)
        => Mutate(playerId, (s, _, _) =>
        {
            s.Phase = "betting";
            s.Status = "Bet on which planet will survive the longest.";
            s.BetOnPlanet = null;
            s.Trajectory = null;
            s.EliminatedOrder = Array.Empty<string>();
            s.WinnerPlanet = null;
            s.LastRoundResult = null;
        }, preloadedPlayer);

    private sealed record TrajectoryResult(Frame[] Frames, string[] EliminatedOrder, string? WinnerPlanet);

    private TrajectoryResult ComputeTrajectory()
    {
        var planets = PlanetTemplates.Select(t => new PlanetState
        {
            Name = t.Name,
            Color = t.Color,
            Mass = t.Mass,
            Radius = t.Radius,
            X = 0, Y = 0, Vx = 0, Vy = 0,
            Alive = true
        }).ToArray();

        InitializePositions(planets);

        var frames = new List<Frame>();
        var eliminatedOrder = new List<string>();
        var eliminatedSet = new HashSet<string>();

        for (var step = 0; step < MaxPhysicsSteps; step++)
        {
            if (step % RecordEvery == 0)
            {
                frames.Add(new Frame
                {
                    Planets = planets.Select(p => new PlanetData(
                        p.Name, p.Color, p.Mass, p.Radius,
                        Math.Round(p.X, 2), Math.Round(p.Y, 2))).ToArray()
                });
            }

            StepRK4(planets);

            // Check ejections
            for (var i = 0; i < planets.Length; i++)
            {
                if (!planets[i].Alive) continue;
                if (Math.Sqrt(planets[i].X * planets[i].X + planets[i].Y * planets[i].Y) > EjectionRadius)
                {
                    planets[i].Alive = false;
                    if (!eliminatedSet.Contains(planets[i].Name))
                    {
                        eliminatedOrder.Add(planets[i].Name);
                        eliminatedSet.Add(planets[i].Name);
                    }
                }
            }

            // Check collisions
            for (var i = 0; i < planets.Length; i++)
            {
                if (!planets[i].Alive) continue;
                for (var j = i + 1; j < planets.Length; j++)
                {
                    if (!planets[j].Alive) continue;
                    var dx = planets[i].X - planets[j].X;
                    var dy = planets[i].Y - planets[j].Y;
                    var dist = Math.Sqrt(dx * dx + dy * dy);
                    var collisionDist = (planets[i].Radius + planets[j].Radius) * CollisionFactor;
                    if (dist < collisionDist)
                    {
                        if (!eliminatedSet.Contains(planets[i].Name))
                        {
                            planets[i].Alive = false;
                            eliminatedOrder.Add(planets[i].Name);
                            eliminatedSet.Add(planets[i].Name);
                        }
                        if (!eliminatedSet.Contains(planets[j].Name))
                        {
                            planets[j].Alive = false;
                            eliminatedOrder.Add(planets[j].Name);
                            eliminatedSet.Add(planets[j].Name);
                        }
                    }
                }
            }

            var aliveCount = planets.Count(p => p.Alive);
            if (aliveCount <= 1)
            {
                // Record final state
                frames.Add(new Frame
                {
                    Planets = planets.Select(p => new PlanetData(
                        p.Name, p.Color, p.Mass, p.Radius,
                        Math.Round(p.X, 2), Math.Round(p.Y, 2))).ToArray()
                });
                break;
            }
        }

        var winner = planets.Count(p => p.Alive) == 1
            ? planets.First(p => p.Alive).Name
            : null;

        if (winner is not null)
            eliminatedOrder.Add(winner);
        else
            eliminatedOrder.Add(""); // draw

        if (frames.Count > 0)
        {
            var last = frames[^1];
            for (var i = 0; i < 45; i++)
                frames.Add(last);
        }

        return new TrajectoryResult(frames.ToArray(), eliminatedOrder.ToArray(), winner);
    }

    private void InitializePositions(PlanetState[] planets)
    {
        var configs = new (double angle, double dist, double speed)[][] {
            new[] { (0.0,  220.0, 5.0), (2.3,  240.0, -4.5), (4.5,  200.0, 6.0) },
            new[] { (1.0,  230.0, 4.0), (3.1,  190.0, -6.0), (5.2,  250.0, -3.5) },
            new[] { (0.5,  200.0, -7.0), (2.7,  260.0, 3.0), (4.8,  220.0, -5.5) },
            new[] { (1.8,  240.0, 4.5), (3.9,  210.0, -5.0), (0.2,  230.0, 6.5) },
            new[] { (0.3,  250.0, -3.0), (2.5,  200.0, 7.0), (5.0,  240.0, -4.0) },
            new[] { (1.4,  210.0, 5.5), (3.6,  240.0, -3.5), (5.7,  190.0, 4.0) },
        };

        var config = configs[_rng.Next(configs.Length)];

        var cx = _rng.NextDouble() * 80 - 40;
        var cy = _rng.NextDouble() * 80 - 40;

        for (var i = 0; i < planets.Length; i++)
        {
            var angle = config[i].angle + (_rng.NextDouble() - 0.5) * 0.5;
            var dist = config[i].dist + (_rng.NextDouble() - 0.5) * 50;
            var speed = config[i].speed + (_rng.NextDouble() - 0.5) * 2.0;
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

    private static void ResolveRound(GameSession s, CasinoDbContext db, Player p)
    {
        if (s.EliminatedOrder.Length == 0) return;
        var winner = s.WinnerPlanet;
        string result;
        decimal payout = 0;

        if (s.BetOnPlanet == winner)
        {
            result = "win";
            payout = s.SelectedBet * 3m;
            p.Balance += payout;
            AddTransaction(db, p.Id, TransactionType.Win, payout);
            s.Wins++;
            s.Status = $"Planet {winner} survived the longest! You win ${payout:0.##}!";
        }
        else
        {
            result = "loss";
            s.Losses++;
            s.Status = winner is not null
                ? $"Planet {winner} survived the longest. You bet on {s.BetOnPlanet}. You lose."
                : "All planets destroyed! You lose.";
        }

        s.LastRoundResult = result;
        s.Phase = "round-over";
    }

    private ThreeBodyStateDTO Mutate(int playerId, Action<GameSession, CasinoDbContext, Player> action, Player? preloaded)
    {
        lock (_sync)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CasinoDbContext>();

            var now = DateTime.UtcNow;
            foreach (var kv in _sessions.Where(kv => now - kv.Value.LastSeenUtc > SessionTimeout).ToList())
            {
                if (kv.Value.Phase == "simulating" && kv.Value.Trajectory is not null)
                {
                    var stalePlayer = db.Players.Find(kv.Key);
                    if (stalePlayer is not null)
                        ResolveRound(kv.Value, db, stalePlayer);
                }
                _sessions.Remove(kv.Key);
            }

            if (!_sessions.TryGetValue(playerId, out var session))
            {
                session = new GameSession();
                _sessions[playerId] = session;
            }
            session.LastSeenUtc = now;

            var player = preloaded ?? db.Players.Find(playerId)
                ?? throw new InvalidOperationException($"Player {playerId} not found.");

            action(session, db, player);
            db.SaveChanges();
            session.Version++;
            return BuildView(session, player);
        }
    }

    private static ThreeBodyStateDTO BuildView(GameSession s, Player p)
    {
        int currentFrame;
        bool isSimulating = s.Phase == "simulating" && s.Trajectory is not null;

        if (isSimulating)
        {
            var elapsed = (DateTime.UtcNow - s.SimulationStartedAt).TotalSeconds;
            var naturalEnd = (s.Trajectory!.Length - 45) / PlaybackFps;
            if (elapsed >= naturalEnd)
            {
                currentFrame = s.Trajectory.Length - 1;
            }
            else
            {
                currentFrame = (int)(elapsed * PlaybackFps);
                if (currentFrame >= s.Trajectory.Length)
                    currentFrame = s.Trajectory.Length - 1;
            }
        }
        else
        {
            currentFrame = s.Phase == "round-over" && s.Trajectory is not null
                ? s.Trajectory.Length - 1
                : 0;
        }

        var currentPlanets = s.Trajectory is not null && s.Trajectory.Length > 0
            ? s.Trajectory[Math.Min(currentFrame, s.Trajectory.Length - 1)].Planets.ToList()
            : PlanetTemplates.Select(t => new PlanetData(t.Name, t.Color, t.Mass, t.Radius, 0, 0)).ToList();

        var allNames = PlanetTemplates.Select(t => t.Name).ToList();

        List<string> aliveNames;
        if (s.Phase == "betting")
        {
            aliveNames = allNames;
        }
        else if (s.Phase == "round-over" && s.WinnerPlanet is not null)
        {
            aliveNames = new List<string> { s.WinnerPlanet };
        }
        else
        {
            var dead = s.EliminatedOrder.Take(s.EliminatedOrder.Length - 1).ToHashSet();
            aliveNames = allNames.Where(n => !dead.Contains(n)).ToList();
        }

        var eliminatedOrder = s.EliminatedOrder.Length > 0
            ? s.EliminatedOrder.Take(s.EliminatedOrder.Length - 1).ToList()
            : new List<string>();

        return new ThreeBodyStateDTO(
            Version: s.Version,
            Phase: s.Phase,
            Status: s.Status,
            PlayerName: $"{p.FirstName} {p.LastName}".Trim(),
            Balance: p.Balance,
            SelectedBet: s.SelectedBet,
            BetOnPlanet: s.BetOnPlanet,
            Planets: currentPlanets,
            CurrentFrame: currentFrame,
            TotalFrames: s.Trajectory?.Length ?? 0,
            AlivePlanets: aliveNames,
            EliminatedOrder: eliminatedOrder,
            WinnerPlanet: s.WinnerPlanet,
            LastRoundResult: s.LastRoundResult,
            Wins: s.Wins,
            Losses: s.Losses,
            CanBet: s.Phase == "betting",
            CanStart: s.Phase == "betting" && s.BetOnPlanet is not null && s.SelectedBet <= p.Balance,
            CanReset: s.Phase == "round-over");
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
