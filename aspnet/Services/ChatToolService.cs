using System.Text.Json;
using aspnet.Data;
using aspnet.Models;
using Microsoft.EntityFrameworkCore;

namespace aspnet.Services;

/// Tko je pozvao chat — prava se određuju ovdje, na serveru, nikad u modelu
public record ChatUserContext(bool IsAuthenticated, bool IsStaff, string? Email);

/// Alati koje DeepSeek model smije pozivati kroz function calling.
/// Lista dostupnih alata ovisi o pravima korisnika, a svaki alat prava
/// provjerava i kod izvršavanja — model ne može zaobići autorizaciju.
/// Mutirajući alati ponašaju se kao postojeći API (transakcija ne mijenja
/// saldo automatski; rezervacija ne provjerava zauzetost stola).
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
                "Get the signed-in user's own transactions. Returns at most 50 rows; for totals over " +
                "the full history use summarize_my_transactions.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        limit = new { type = "integer", description = "Max number of rows, default 20, max 50." },
                        transactionType = new { type = "string", @enum = TransactionTypeNames, description = "Filter by type." },
                        sortBy = new { type = "string", @enum = new[] { "date", "amount" }, description = "Sort key, default date." },
                        order = new { type = "string", @enum = new[] { "asc", "desc" }, description = "Sort order, default desc." }
                    }
                }));
            tools.Add(Tool("summarize_my_transactions",
                "Sums and counts over the signed-in user's COMPLETE transaction history, grouped by type. " +
                "Use this for questions about totals.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        from = new { type = "string", description = "ISO date, include transactions from this date." },
                        to = new { type = "string", description = "ISO date, include transactions up to this date." }
                    }
                }));
            tools.Add(Tool("get_my_reservations",
                "Get the signed-in user's own table reservations with casino and game info."));
            tools.Add(Tool("create_my_reservation",
                "Create a table reservation for the signed-in user. Only call when the user explicitly asks to reserve.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tableId = new { type = "integer", description = "Table id (see list_tables)." },
                        reservedAt = new { type = "string", description = "ISO date-time of the reservation; defaults to now." }
                    },
                    required = new[] { "tableId" }
                }));
            tools.Add(Tool("cancel_my_reservation",
                "Cancel one of the signed-in user's own reservations. Only call when the user explicitly asks.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        reservationId = new { type = "integer", description = "Reservation id (see get_my_reservations)." }
                    },
                    required = new[] { "reservationId" }
                }));
        }

        if (user.IsStaff)
        {
            tools.Add(Tool("find_players", "Staff only: search players by name or email.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        query = new { type = "string", description = "Substring of name or email; omit to list all." },
                        sortBy = new { type = "string", @enum = new[] { "name", "balance" }, description = "Sort key, default name." },
                        order = new { type = "string", @enum = new[] { "asc", "desc" }, description = "Sort order, default asc." }
                    }
                }));
            tools.Add(Tool("get_player_details",
                "Staff only: full details of one player — profile, balance, the 20 most recent transactions " +
                "and reservations. For totals over full history use summarize_transactions.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        playerId = new { type = "integer", description = "Player id." }
                    },
                    required = new[] { "playerId" }
                }));
            tools.Add(Tool("summarize_transactions",
                "Staff only: sums and counts over COMPLETE transaction history, grouped by type. " +
                "Omit playerId for casino-wide totals across all players.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        playerId = new { type = "integer", description = "Limit to one player; omit for all players." },
                        from = new { type = "string", description = "ISO date, include transactions from this date." },
                        to = new { type = "string", description = "ISO date, include transactions up to this date." }
                    }
                }));
            tools.Add(Tool("top_players",
                "Staff only: rank players by a metric over their complete history.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        metric = new
                        {
                            type = "string",
                            @enum = new[] { "balance", "total_bets", "total_deposits", "total_wins", "total_withdrawals" },
                            description = "Ranking metric."
                        },
                        limit = new { type = "integer", description = "How many players, default 5, max 20." }
                    },
                    required = new[] { "metric" }
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
            tools.Add(Tool("create_transaction",
                "Staff only: record a transaction for a player. Does NOT change the player's balance " +
                "(same as the rest of the app); use update_player to adjust balance. " +
                "Only call when the user explicitly asks.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        playerId = new { type = "integer", description = "Player id." },
                        amount = new { type = "number", description = "Amount, 0.01 to 1000000." },
                        transactionType = new { type = "string", @enum = TransactionTypeNames, description = "Transaction type." }
                    },
                    required = new[] { "playerId", "amount", "transactionType" }
                }));
            tools.Add(Tool("update_player",
                "Staff only: update a player's profile fields and/or balance. Only provided fields change. " +
                "Only call when the user explicitly asks.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        playerId = new { type = "integer", description = "Player id." },
                        firstName = new { type = "string" },
                        lastName = new { type = "string" },
                        email = new { type = "string" },
                        balance = new { type = "number", description = "New balance, must be 0 or greater." }
                    },
                    required = new[] { "playerId" }
                }));
            tools.Add(Tool("update_table",
                "Staff only: update a table's availability and/or bet range. Only provided fields change. " +
                "Only call when the user explicitly asks.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tableId = new { type = "integer", description = "Table id." },
                        isAvailable = new { type = "boolean" },
                        minBet = new { type = "number" },
                        maxBet = new { type = "number" }
                    },
                    required = new[] { "tableId" }
                }));
            tools.Add(Tool("create_reservation",
                "Staff only: create a reservation for any player. Only call when the user explicitly asks.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        playerId = new { type = "integer", description = "Player id." },
                        tableId = new { type = "integer", description = "Table id." },
                        reservedAt = new { type = "string", description = "ISO date-time; defaults to now." }
                    },
                    required = new[] { "playerId", "tableId" }
                }));
            tools.Add(Tool("delete_reservation",
                "Staff only: delete any reservation by id. Only call when the user explicitly asks.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        reservationId = new { type = "integer", description = "Reservation id." }
                    },
                    required = new[] { "reservationId" }
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

                var query = _db.Transactions.Where(t => t.PlayerId == player.Id);
                if (ParseTransactionType(GetString(args, "transactionType")) is { } type)
                {
                    query = query.Where(t => t.Type == type);
                }

                var ascending = GetString(args, "order") == "asc";
                query = GetString(args, "sortBy") == "amount"
                    ? (ascending ? query.OrderBy(t => t.Amount) : query.OrderByDescending(t => t.Amount))
                    : (ascending ? query.OrderBy(t => t.CreatedAt) : query.OrderByDescending(t => t.CreatedAt));

                var transactions = await query.Take(limit).ToListAsync();
                return Json(transactions.Select(t => new
                {
                    t.Id, t.Amount, Type = t.Type.ToString(), t.CreatedAt
                }));
            }

            case "summarize_my_transactions":
            {
                if (await FindOwnPlayerAsync(user) is not { } player) return NoPlayerProfile(user);
                return await SummarizeTransactionsAsync(player.Id, args);
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

            case "create_my_reservation":
            {
                if (!user.IsAuthenticated) return Forbidden();
                if (await FindOwnPlayerAsync(user) is not { } player) return NoPlayerProfile(user);
                if (GetInt(args, "tableId") is not int tableId) return Error("tableId is required.");
                return await CreateReservationAsync(player.Id, tableId, GetDate(args, "reservedAt"));
            }

            case "cancel_my_reservation":
            {
                if (!user.IsAuthenticated) return Forbidden();
                if (await FindOwnPlayerAsync(user) is not { } player) return NoPlayerProfile(user);
                if (GetInt(args, "reservationId") is not int reservationId) return Error("reservationId is required.");

                var reservation = await _db.Reservations
                    .Include(r => r.Table)
                    .FirstOrDefaultAsync(r => r.Id == reservationId);
                if (reservation is null) return Error($"Reservation {reservationId} not found.");
                // Vlastite rezervacije smiju se otkazati, tuđe ne
                if (reservation.PlayerId != player.Id) return Error("Access denied: that reservation belongs to another player.");

                _db.Reservations.Remove(reservation);
                await _db.SaveChangesAsync();
                return Json(new { ok = true, cancelledReservationId = reservationId });
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

                var descending = GetString(args, "order") == "desc";
                query = GetString(args, "sortBy") == "balance"
                    ? (descending ? query.OrderByDescending(p => p.Balance) : query.OrderBy(p => p.Balance))
                    : (descending
                        ? query.OrderByDescending(p => p.LastName).ThenByDescending(p => p.FirstName)
                        : query.OrderBy(p => p.LastName).ThenBy(p => p.FirstName));

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

            case "summarize_transactions":
            {
                if (!user.IsStaff) return Forbidden();
                return await SummarizeTransactionsAsync(GetInt(args, "playerId"), args);
            }

            case "top_players":
            {
                if (!user.IsStaff) return Forbidden();
                var limit = Math.Clamp(GetInt(args, "limit") ?? 5, 1, 20);
                var metric = GetString(args, "metric") ?? "balance";

                if (metric == "balance")
                {
                    var byBalance = await _db.Players
                        .OrderByDescending(p => p.Balance)
                        .Take(limit)
                        .Select(p => new { p.Id, p.FirstName, p.LastName, Value = p.Balance })
                        .ToListAsync();
                    return Json(new { metric, players = byBalance });
                }

                var type = metric switch
                {
                    "total_bets" => TransactionType.Bet,
                    "total_deposits" => TransactionType.Deposit,
                    "total_wins" => TransactionType.Win,
                    "total_withdrawals" => TransactionType.Withdrawal,
                    _ => (TransactionType?)null
                };
                if (type is null) return Error($"Unknown metric '{metric}'.");

                var ranked = await _db.Transactions
                    .Where(t => t.Type == type)
                    .GroupBy(t => new { t.PlayerId, t.Player.FirstName, t.Player.LastName })
                    .Select(g => new
                    {
                        Id = g.Key.PlayerId, g.Key.FirstName, g.Key.LastName,
                        Value = g.Sum(t => t.Amount), TransactionCount = g.Count()
                    })
                    .OrderByDescending(x => x.Value)
                    .Take(limit)
                    .ToListAsync();
                return Json(new { metric, players = ranked });
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

            case "create_transaction":
            {
                if (!user.IsStaff) return Forbidden();
                if (GetInt(args, "playerId") is not int playerId) return Error("playerId is required.");
                if (GetDecimal(args, "amount") is not decimal amount) return Error("amount is required.");
                if (amount is < 0.01m or > 1_000_000m) return Error("amount must be between 0.01 and 1000000.");
                if (ParseTransactionType(GetString(args, "transactionType")) is not { } type)
                {
                    return Error($"transactionType must be one of: {string.Join(", ", TransactionTypeNames)}.");
                }
                if (!await _db.Players.AnyAsync(p => p.Id == playerId)) return Error($"Player {playerId} not found.");

                var transaction = new Transaction
                {
                    Amount = amount, Type = type, CreatedAt = DateTime.Now, PlayerId = playerId
                };
                _db.Transactions.Add(transaction);
                await _db.SaveChangesAsync();
                return Json(new
                {
                    ok = true, transaction.Id, transaction.Amount,
                    Type = transaction.Type.ToString(), transaction.CreatedAt, transaction.PlayerId
                });
            }

            case "update_player":
            {
                if (!user.IsStaff) return Forbidden();
                if (GetInt(args, "playerId") is not int playerId) return Error("playerId is required.");
                var player = await _db.Players.FirstOrDefaultAsync(p => p.Id == playerId);
                if (player is null) return Error($"Player {playerId} not found.");

                if (GetString(args, "firstName") is { Length: > 0 } firstName) player.FirstName = firstName;
                if (GetString(args, "lastName") is { Length: > 0 } lastName) player.LastName = lastName;
                if (GetString(args, "email") is { Length: > 0 } email)
                {
                    if (!email.Contains('@')) return Error("email is not a valid address.");
                    player.Email = email;
                }
                if (GetDecimal(args, "balance") is decimal balance)
                {
                    if (balance < 0) return Error("balance must be 0 or greater.");
                    player.Balance = balance;
                }

                await _db.SaveChangesAsync();
                return Json(new
                {
                    ok = true, player.Id, player.FirstName, player.LastName, player.Email, player.Balance
                });
            }

            case "update_table":
            {
                if (!user.IsStaff) return Forbidden();
                if (GetInt(args, "tableId") is not int tableId) return Error("tableId is required.");
                var table = await _db.Tables.FirstOrDefaultAsync(t => t.Id == tableId);
                if (table is null) return Error($"Table {tableId} not found.");

                if (GetBool(args, "isAvailable") is bool isAvailable) table.IsAvailable = isAvailable;
                if (GetDecimal(args, "minBet") is decimal minBet) table.MinBet = minBet;
                if (GetDecimal(args, "maxBet") is decimal maxBet) table.MaxBet = maxBet;
                if (table.MinBet is < 0.01m or > 1_000_000m || table.MaxBet is < 0.01m or > 1_000_000m)
                {
                    return Error("Bets must be between 0.01 and 1000000.");
                }
                if (table.MinBet > table.MaxBet) return Error("minBet cannot be greater than maxBet.");

                await _db.SaveChangesAsync();
                return Json(new
                {
                    ok = true, table.Id, table.TableNumber, table.IsAvailable, table.MinBet, table.MaxBet
                });
            }

            case "create_reservation":
            {
                if (!user.IsStaff) return Forbidden();
                if (GetInt(args, "playerId") is not int playerId) return Error("playerId is required.");
                if (GetInt(args, "tableId") is not int tableId) return Error("tableId is required.");
                if (!await _db.Players.AnyAsync(p => p.Id == playerId)) return Error($"Player {playerId} not found.");
                return await CreateReservationAsync(playerId, tableId, GetDate(args, "reservedAt"));
            }

            case "delete_reservation":
            {
                if (!user.IsStaff) return Forbidden();
                if (GetInt(args, "reservationId") is not int reservationId) return Error("reservationId is required.");
                var reservation = await _db.Reservations.FirstOrDefaultAsync(r => r.Id == reservationId);
                if (reservation is null) return Error($"Reservation {reservationId} not found.");

                _db.Reservations.Remove(reservation);
                await _db.SaveChangesAsync();
                return Json(new { ok = true, deletedReservationId = reservationId });
            }

            default:
                return Error($"Unknown tool '{name}'.");
        }
    }

    /// Sume i brojevi po tipu preko cijele povijesti; playerId == null znači svi igrači
    private async Task<string> SummarizeTransactionsAsync(int? playerId, JsonElement args)
    {
        var query = _db.Transactions.AsQueryable();
        if (playerId is int id) query = query.Where(t => t.PlayerId == id);
        if (GetDate(args, "from") is DateTime from) query = query.Where(t => t.CreatedAt >= from);
        if (GetDate(args, "to") is DateTime to) query = query.Where(t => t.CreatedAt <= to);

        var byType = await query
            .GroupBy(t => t.Type)
            .Select(g => new
            {
                Type = g.Key, Count = g.Count(), Total = g.Sum(t => t.Amount),
                First = g.Min(t => t.CreatedAt), Last = g.Max(t => t.CreatedAt)
            })
            .ToListAsync();

        return Json(new
        {
            playerId,
            scope = playerId is null ? "all players" : "one player",
            totalCount = byType.Sum(x => x.Count),
            byType = byType.Select(x => new
            {
                Type = x.Type.ToString(), x.Count, x.Total, x.First, x.Last
            })
        });
    }

    private async Task<string> CreateReservationAsync(int playerId, int tableId, DateTime? reservedAt)
    {
        var table = await _db.Tables
            .Include(t => t.Casino).Include(t => t.Game)
            .FirstOrDefaultAsync(t => t.Id == tableId);
        if (table is null) return Error($"Table {tableId} not found.");

        var reservation = new Reservation
        {
            ReservedAt = reservedAt ?? DateTime.Now,
            PlayerId = playerId,
            TableId = tableId
        };
        _db.Reservations.Add(reservation);
        await _db.SaveChangesAsync();

        return Json(new
        {
            ok = true, reservation.Id, reservation.ReservedAt,
            table.TableNumber, Casino = table.Casino.Name, Game = table.Game.Name
        });
    }

    /// Player račun povezan s prijavljenim korisnikom — veza je email (UserName == Email)
    private async Task<Models.Player?> FindOwnPlayerAsync(ChatUserContext user)
    {
        if (string.IsNullOrEmpty(user.Email)) return null;
        return await _db.Players.FirstOrDefaultAsync(p => p.Email == user.Email);
    }

    private static readonly string[] TransactionTypeNames = Enum.GetNames<TransactionType>();

    private static TransactionType? ParseTransactionType(string? value) =>
        Enum.TryParse<TransactionType>(value, ignoreCase: true, out var t) ? t : null;

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

    private static decimal? GetDecimal(JsonElement args, string property) =>
        args.ValueKind == JsonValueKind.Object &&
        args.TryGetProperty(property, out var v) &&
        v.ValueKind == JsonValueKind.Number && v.TryGetDecimal(out var d) ? d : null;

    private static bool? GetBool(JsonElement args, string property) =>
        args.ValueKind == JsonValueKind.Object && args.TryGetProperty(property, out var v) &&
        v.ValueKind is JsonValueKind.True or JsonValueKind.False ? v.GetBoolean() : null;

    private static string? GetString(JsonElement args, string property) =>
        args.ValueKind == JsonValueKind.Object && args.TryGetProperty(property, out var v) &&
        v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static DateTime? GetDate(JsonElement args, string property) =>
        GetString(args, property) is { } s && DateTime.TryParse(s, out var d) ? d : null;
}
