using System.Text.Json;
using aspnet.Data;
using Microsoft.EntityFrameworkCore;

namespace aspnet.Services;

/// Tko je pozvao chat — prava se određuju ovdje, na serveru, nikad u modelu
public record ChatUserContext(bool IsAuthenticated, bool IsStaff, string? Email);

/// Read-only alati koje DeepSeek model smije pozivati kroz function calling.
/// Lista dostupnih alata ovisi o pravima korisnika, a svaki alat prava
/// provjerava i kod izvršavanja — model ne može zaobići autorizaciju.
public class ChatToolService
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    private readonly CasinoDbContext _db;

    public ChatToolService(CasinoDbContext db) => _db = db;

    public List<object> GetToolDefinitions(ChatUserContext user)
    {
        var tools = new List<object>
        {
            Tool("list_casinos", "List all casinos with address, license number and founding date."),
            Tool("list_games", "List all games with type, bet range and description."),
            Tool("list_tables", "List casino tables with game, bet range and availability.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        casinoId = new { type = "integer", description = "Filter by casino id." },
                        onlyAvailable = new { type = "boolean", description = "Return only currently available tables." }
                    }
                })
        };

        if (user.IsAuthenticated)
        {
            tools.Add(Tool("get_my_profile",
                "Get the signed-in user's own player profile: name, email, date of birth and current balance."));
            tools.Add(Tool("get_my_transactions",
                "Get the signed-in user's own recent transactions (deposits, withdrawals, bets, wins).",
                new
                {
                    type = "object",
                    properties = new
                    {
                        limit = new { type = "integer", description = "Max number of transactions, default 20, max 50." }
                    }
                }));
            tools.Add(Tool("get_my_reservations",
                "Get the signed-in user's own table reservations with casino and game info."));
        }

        if (user.IsStaff)
        {
            tools.Add(Tool("find_players", "Staff only: search players by name or email.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        query = new { type = "string", description = "Substring of name or email; omit to list all." }
                    }
                }));
            tools.Add(Tool("get_player_details",
                "Staff only: full details of one player — profile, balance, recent transactions and reservations.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        playerId = new { type = "integer", description = "Player id." }
                    },
                    required = new[] { "playerId" }
                }));
            tools.Add(Tool("list_employees", "Staff only: list employees with position and casino.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        casinoId = new { type = "integer", description = "Filter by casino id." }
                    }
                }));
        }

        return tools;
    }

    public async Task<string> ExecuteAsync(string name, JsonElement args, ChatUserContext user)
    {
        switch (name)
        {
            case "list_casinos":
            {
                var casinos = await _db.Casinos
                    .Select(c => new { c.Id, c.Name, c.Address, c.LicenseNumber, c.FoundedDate })
                    .ToListAsync();
                return Json(casinos);
            }

            case "list_games":
            {
                var games = await _db.Games.ToListAsync();
                return Json(games.Select(g => new
                {
                    g.Id, g.Name, Type = g.Type.ToString(), g.MinBet, g.MaxBet, g.Description
                }));
            }

            case "list_tables":
            {
                var query = _db.Tables.Include(t => t.Casino).Include(t => t.Game).AsQueryable();
                if (GetInt(args, "casinoId") is int casinoId) query = query.Where(t => t.CasinoId == casinoId);
                if (GetBool(args, "onlyAvailable") == true) query = query.Where(t => t.IsAvailable);

                var tables = await query.Take(100).ToListAsync();
                return Json(tables.Select(t => new
                {
                    t.Id, t.TableNumber, Casino = t.Casino.Name, Game = t.Game.Name,
                    t.MinBet, t.MaxBet, t.IsAvailable
                }));
            }

            case "get_my_profile":
            {
                if (await FindOwnPlayerAsync(user) is not { } player) return NoPlayerProfile(user);
                return Json(new
                {
                    player.Id, player.FirstName, player.LastName, player.Email,
                    DateOfBirth = player.DateOfBirth.Date, player.Balance
                });
            }

            case "get_my_transactions":
            {
                if (await FindOwnPlayerAsync(user) is not { } player) return NoPlayerProfile(user);
                var limit = Math.Clamp(GetInt(args, "limit") ?? 20, 1, 50);
                var transactions = await _db.Transactions
                    .Where(t => t.PlayerId == player.Id)
                    .OrderByDescending(t => t.CreatedAt)
                    .Take(limit)
                    .ToListAsync();
                return Json(transactions.Select(t => new
                {
                    t.Id, t.Amount, Type = t.Type.ToString(), t.CreatedAt
                }));
            }

            case "get_my_reservations":
            {
                if (await FindOwnPlayerAsync(user) is not { } player) return NoPlayerProfile(user);
                var reservations = await _db.Reservations
                    .Include(r => r.Table).ThenInclude(t => t.Casino)
                    .Include(r => r.Table).ThenInclude(t => t.Game)
                    .Where(r => r.PlayerId == player.Id)
                    .OrderByDescending(r => r.ReservedAt)
                    .Take(50)
                    .ToListAsync();
                return Json(reservations.Select(r => new
                {
                    r.Id, r.ReservedAt, TableNumber = r.Table.TableNumber,
                    Casino = r.Table.Casino.Name, Game = r.Table.Game.Name
                }));
            }

            case "find_players":
            {
                if (!user.IsStaff) return Forbidden();
                var query = _db.Players.AsQueryable();
                if (GetString(args, "query") is { Length: > 0 } q)
                {
                    query = query.Where(p =>
                        p.FirstName.Contains(q) || p.LastName.Contains(q) || p.Email.Contains(q));
                }
                var players = await query.Take(50).ToListAsync();
                return Json(players.Select(p => new
                {
                    p.Id, p.FirstName, p.LastName, p.Email, p.Balance
                }));
            }

            case "get_player_details":
            {
                if (!user.IsStaff) return Forbidden();
                if (GetInt(args, "playerId") is not int playerId) return Error("playerId is required.");

                var player = await _db.Players
                    .Include(p => p.Transactions)
                    .Include(p => p.Reservations).ThenInclude(r => r.Table).ThenInclude(t => t.Casino)
                    .FirstOrDefaultAsync(p => p.Id == playerId);
                if (player is null) return Error($"Player {playerId} not found.");

                return Json(new
                {
                    player.Id, player.FirstName, player.LastName, player.Email,
                    DateOfBirth = player.DateOfBirth.Date, player.Balance,
                    RecentTransactions = player.Transactions
                        .OrderByDescending(t => t.CreatedAt)
                        .Take(20)
                        .Select(t => new { t.Id, t.Amount, Type = t.Type.ToString(), t.CreatedAt }),
                    Reservations = player.Reservations
                        .OrderByDescending(r => r.ReservedAt)
                        .Take(20)
                        .Select(r => new
                        {
                            r.Id, r.ReservedAt, r.Table.TableNumber, Casino = r.Table.Casino.Name
                        })
                });
            }

            case "list_employees":
            {
                if (!user.IsStaff) return Forbidden();
                var query = _db.Employees.Include(e => e.Casino).AsQueryable();
                if (GetInt(args, "casinoId") is int casinoId) query = query.Where(e => e.CasinoId == casinoId);

                var employees = await query.Take(100).ToListAsync();
                return Json(employees.Select(e => new
                {
                    e.Id, e.FirstName, e.LastName, e.Position, Casino = e.Casino.Name
                }));
            }

            default:
                return Error($"Unknown tool '{name}'.");
        }
    }

    /// Player račun povezan s prijavljenim korisnikom — veza je email (UserName == Email)
    private async Task<Models.Player?> FindOwnPlayerAsync(ChatUserContext user)
    {
        if (string.IsNullOrEmpty(user.Email)) return null;
        return await _db.Players.FirstOrDefaultAsync(p => p.Email == user.Email);
    }

    private static string NoPlayerProfile(ChatUserContext user) =>
        Error($"No player profile is linked to the account email '{user.Email}'.");

    private static string Forbidden() => Error("Access denied: this tool is for staff only.");

    private static string Error(string message) => Json(new { error = message });

    private static string Json(object value) => JsonSerializer.Serialize(value, JsonOpts);

    private static object Tool(string name, string description, object? parameters = null) => new
    {
        type = "function",
        function = new
        {
            name,
            description,
            parameters = parameters ?? new { type = "object", properties = new { } }
        }
    };

    private static int? GetInt(JsonElement args, string property) =>
        args.ValueKind == JsonValueKind.Object &&
        args.TryGetProperty(property, out var v) && v.TryGetInt32(out var i) ? i : null;

    private static bool? GetBool(JsonElement args, string property) =>
        args.ValueKind == JsonValueKind.Object && args.TryGetProperty(property, out var v) &&
        v.ValueKind is JsonValueKind.True or JsonValueKind.False ? v.GetBoolean() : null;

    private static string? GetString(JsonElement args, string property) =>
        args.ValueKind == JsonValueKind.Object && args.TryGetProperty(property, out var v) &&
        v.ValueKind == JsonValueKind.String ? v.GetString() : null;
}
