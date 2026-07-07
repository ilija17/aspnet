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

            // ── Casino CRUD ───────────────────────────────────────────────────
            tools.Add(Tool("create_casino",
                "Staff only: create a new casino. Only call when the user explicitly asks.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        name = new { type = "string", description = "Casino name, max 200 characters." },
                        address = new { type = "string", description = "Full address, max 300 characters." },
                        licenseNumber = new { type = "string", description = "License number, max 50 characters." },
                        foundedDate = new { type = "string", description = "ISO date of founding, e.g. 2020-01-15." }
                    },
                    required = new[] { "name", "address", "licenseNumber", "foundedDate" }
                }));
            tools.Add(Tool("update_casino",
                "Staff only: update casino fields. Only provided fields change. Only call when the user explicitly asks.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        casinoId = new { type = "integer", description = "Casino id." },
                        name = new { type = "string", description = "New name, max 200 characters." },
                        address = new { type = "string", description = "New address, max 300 characters." },
                        licenseNumber = new { type = "string", description = "New license number, max 50 characters." },
                        foundedDate = new { type = "string", description = "New ISO founding date." }
                    },
                    required = new[] { "casinoId" }
                }));
            tools.Add(Tool("delete_casino",
                "Staff only: delete a casino by id. Cannot be undone. Only call when the user explicitly asks.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        casinoId = new { type = "integer", description = "Casino id to delete." }
                    },
                    required = new[] { "casinoId" }
                }));

            // ── Game CRUD ─────────────────────────────────────────────────────
            tools.Add(Tool("create_game",
                "Staff only: create a new game type. Only call when the user explicitly asks.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        name = new { type = "string", description = "Game name, max 100 characters." },
                        type = new { type = "string", @enum = GameTypeNames, description = "Game type." },
                        minBet = new { type = "number", description = "Minimum bet, 0.01 to 1000000. Default 1." },
                        maxBet = new { type = "number", description = "Maximum bet, 0.01 to 1000000. Default 100." },
                        description = new { type = "string", description = "Optional description, max 500 characters." }
                    },
                    required = new[] { "name", "type" }
                }));
            tools.Add(Tool("update_game",
                "Staff only: update game fields. Only provided fields change. Only call when the user explicitly asks.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        gameId = new { type = "integer", description = "Game id." },
                        name = new { type = "string", description = "New name, max 100 characters." },
                        type = new { type = "string", @enum = GameTypeNames, description = "New game type." },
                        minBet = new { type = "number", description = "New minimum bet." },
                        maxBet = new { type = "number", description = "New maximum bet." },
                        description = new { type = "string", description = "New description." }
                    },
                    required = new[] { "gameId" }
                }));
            tools.Add(Tool("delete_game",
                "Staff only: delete a game by id. Cannot be undone. Only call when the user explicitly asks.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        gameId = new { type = "integer", description = "Game id to delete." }
                    },
                    required = new[] { "gameId" }
                }));

            // ── Employee CRUD ──────────────────────────────────────────────────
            tools.Add(Tool("create_employee",
                "Staff only: create a new employee. casinoId must reference an existing casino. " +
                "Only call when the user explicitly asks.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        firstName = new { type = "string", description = "First name, max 100 characters." },
                        lastName = new { type = "string", description = "Last name, max 100 characters." },
                        position = new { type = "string", description = "Job position, e.g. Dealer, Cashier, Manager, Pit Boss." },
                        casinoId = new { type = "integer", description = "Id of the casino they work at." }
                    },
                    required = new[] { "firstName", "lastName", "position", "casinoId" }
                }));
            tools.Add(Tool("update_employee",
                "Staff only: update employee fields. Only provided fields change. Only call when the user explicitly asks.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        employeeId = new { type = "integer", description = "Employee id." },
                        firstName = new { type = "string" },
                        lastName = new { type = "string" },
                        position = new { type = "string" },
                        casinoId = new { type = "integer", description = "Move employee to a different casino." }
                    },
                    required = new[] { "employeeId" }
                }));
            tools.Add(Tool("delete_employee",
                "Staff only: delete an employee by id. Cannot be undone. Only call when the user explicitly asks.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        employeeId = new { type = "integer", description = "Employee id to delete." }
                    },
                    required = new[] { "employeeId" }
                }));

            // ── Table CRUD (create + delete, update already exists) ────────────
            tools.Add(Tool("create_table",
                "Staff only: create a new table in a casino. casinoId and gameId must reference existing records. " +
                "Only call when the user explicitly asks.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tableNumber = new { type = "integer", description = "Table number, must be positive." },
                        casinoId = new { type = "integer", description = "Id of the casino." },
                        gameId = new { type = "integer", description = "Id of the game played at this table." },
                        isAvailable = new { type = "boolean", description = "Whether the table is available. Default true." },
                        minBet = new { type = "number", description = "Minimum bet. Default 10." },
                        maxBet = new { type = "number", description = "Maximum bet. Default 500." }
                    },
                    required = new[] { "tableNumber", "casinoId", "gameId" }
                }));
            tools.Add(Tool("delete_table",
                "Staff only: delete a table by id. Cannot be undone. Only call when the user explicitly asks.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tableId = new { type = "integer", description = "Table id to delete." }
                    },
                    required = new[] { "tableId" }
                }));

            // ── Player CRUD (create new; update already exists) ─────────────────
            tools.Add(Tool("create_player",
                "Staff only: create a new player. Only call when the user explicitly asks.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        firstName = new { type = "string", description = "First name, max 100 characters." },
                        lastName = new { type = "string", description = "Last name, max 100 characters." },
                        email = new { type = "string", description = "Email address (unique per player)." },
                        dateOfBirth = new { type = "string", description = "ISO date of birth, e.g. 1990-05-20." },
                        balance = new { type = "number", description = "Initial balance, must be 0 or greater. Default 0." }
                    },
                    required = new[] { "firstName", "lastName", "email", "dateOfBirth" }
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

            case "create_casino":
            {
                if (!user.IsStaff) return Forbidden();
                if (GetString(args, "name") is not { Length: > 0 } casinoName) return Error("name is required.");
                if (casinoName.Length > 200) return Error("name must be at most 200 characters.");
                if (GetString(args, "address") is not { Length: > 0 } address) return Error("address is required.");
                if (address.Length > 300) return Error("address must be at most 300 characters.");
                if (GetString(args, "licenseNumber") is not { Length: > 0 } licenseNumber) return Error("licenseNumber is required.");
                if (licenseNumber.Length > 50) return Error("licenseNumber must be at most 50 characters.");
                if (GetDate(args, "foundedDate") is not DateTime foundedDate) return Error("foundedDate is required (ISO date).");

                var casino = new Casino
                {
                    Name = casinoName, Address = address, LicenseNumber = licenseNumber, FoundedDate = foundedDate
                };
                _db.Casinos.Add(casino);
                await _db.SaveChangesAsync();
                return Json(new { ok = true, casino.Id, casino.Name, casino.LicenseNumber, casino.FoundedDate });
            }

            case "update_casino":
            {
                if (!user.IsStaff) return Forbidden();
                if (GetInt(args, "casinoId") is not int casinoId) return Error("casinoId is required.");
                var casino = await _db.Casinos.FirstOrDefaultAsync(c => c.Id == casinoId);
                if (casino is null) return Error($"Casino {casinoId} not found.");

                if (GetString(args, "name") is { Length: > 0 } n)
                {
                    if (n.Length > 200) return Error("name must be at most 200 characters.");
                    casino.Name = n;
                }
                if (GetString(args, "address") is { Length: > 0 } a)
                {
                    if (a.Length > 300) return Error("address must be at most 300 characters.");
                    casino.Address = a;
                }
                if (GetString(args, "licenseNumber") is { Length: > 0 } lic)
                {
                    if (lic.Length > 50) return Error("licenseNumber must be at most 50 characters.");
                    casino.LicenseNumber = lic;
                }
                if (GetDate(args, "foundedDate") is DateTime fd) casino.FoundedDate = fd;

                await _db.SaveChangesAsync();
                return Json(new { ok = true, casino.Id, casino.Name, casino.LicenseNumber, casino.FoundedDate });
            }

            case "delete_casino":
            {
                if (!user.IsStaff) return Forbidden();
                if (GetInt(args, "casinoId") is not int casinoId) return Error("casinoId is required.");
                var casino = await _db.Casinos.FirstOrDefaultAsync(c => c.Id == casinoId);
                if (casino is null) return Error($"Casino {casinoId} not found.");
                _db.Casinos.Remove(casino);
                await _db.SaveChangesAsync();
                return Json(new { ok = true, deletedCasinoId = casinoId });
            }

            case "create_game":
            {
                if (!user.IsStaff) return Forbidden();
                if (GetString(args, "name") is not { Length: > 0 } gameName) return Error("name is required.");
                if (gameName.Length > 100) return Error("name must be at most 100 characters.");
                if (ParseGameType(GetString(args, "type")) is not { } gameType) return Error($"type must be one of: {string.Join(", ", GameTypeNames)}.");

                var minBet = GetDecimal(args, "minBet") ?? 1m;
                var maxBet = GetDecimal(args, "maxBet") ?? 100m;
                if (minBet is < 0.01m or > 1_000_000m || maxBet is < 0.01m or > 1_000_000m)
                    return Error("Bets must be between 0.01 and 1000000.");
                if (minBet > maxBet) return Error("minBet cannot be greater than maxBet.");

                var description = GetString(args, "description") ?? string.Empty;
                if (description.Length > 500) return Error("description must be at most 500 characters.");

                var game = new Game
                {
                    Name = gameName, Type = gameType, MinBet = minBet, MaxBet = maxBet, Description = description
                };
                _db.Games.Add(game);
                await _db.SaveChangesAsync();
                return Json(new { ok = true, game.Id, game.Name, Type = game.Type.ToString(), game.MinBet, game.MaxBet, game.Description });
            }

            case "update_game":
            {
                if (!user.IsStaff) return Forbidden();
                if (GetInt(args, "gameId") is not int gameId) return Error("gameId is required.");
                var game = await _db.Games.FirstOrDefaultAsync(g => g.Id == gameId);
                if (game is null) return Error($"Game {gameId} not found.");

                if (GetString(args, "name") is { Length: > 0 } n)
                {
                    if (n.Length > 100) return Error("name must be at most 100 characters.");
                    game.Name = n;
                }
                if (ParseGameType(GetString(args, "type")) is { } gt) game.Type = gt;
                if (GetDecimal(args, "minBet") is decimal mb)
                {
                    if (mb is < 0.01m or > 1_000_000m) return Error("minBet must be between 0.01 and 1000000.");
                    game.MinBet = mb;
                }
                if (GetDecimal(args, "maxBet") is decimal xb)
                {
                    if (xb is < 0.01m or > 1_000_000m) return Error("maxBet must be between 0.01 and 1000000.");
                    game.MaxBet = xb;
                }
                if (game.MinBet > game.MaxBet) return Error("minBet cannot be greater than maxBet.");
                if (GetString(args, "description") is { } desc)
                {
                    if (desc.Length > 500) return Error("description must be at most 500 characters.");
                    game.Description = desc;
                }

                await _db.SaveChangesAsync();
                return Json(new { ok = true, game.Id, game.Name, Type = game.Type.ToString(), game.MinBet, game.MaxBet, game.Description });
            }

            case "delete_game":
            {
                if (!user.IsStaff) return Forbidden();
                if (GetInt(args, "gameId") is not int gameId) return Error("gameId is required.");
                var game = await _db.Games.FirstOrDefaultAsync(g => g.Id == gameId);
                if (game is null) return Error($"Game {gameId} not found.");
                _db.Games.Remove(game);
                await _db.SaveChangesAsync();
                return Json(new { ok = true, deletedGameId = gameId });
            }

            case "create_employee":
            {
                if (!user.IsStaff) return Forbidden();
                if (GetString(args, "firstName") is not { Length: > 0 } firstName) return Error("firstName is required.");
                if (firstName.Length > 100) return Error("firstName must be at most 100 characters.");
                if (GetString(args, "lastName") is not { Length: > 0 } lastName) return Error("lastName is required.");
                if (lastName.Length > 100) return Error("lastName must be at most 100 characters.");
                if (GetString(args, "position") is not { Length: > 0 } position) return Error("position is required.");
                if (position.Length > 100) return Error("position must be at most 100 characters.");
                if (GetInt(args, "casinoId") is not int casinoId) return Error("casinoId is required.");
                if (!await _db.Casinos.AnyAsync(c => c.Id == casinoId)) return Error($"Casino {casinoId} not found.");

                var employee = new Employee
                {
                    FirstName = firstName, LastName = lastName, Position = position, CasinoId = casinoId
                };
                _db.Employees.Add(employee);
                await _db.SaveChangesAsync();
                await _db.Entry(employee).Reference(e => e.Casino).LoadAsync();
                return Json(new { ok = true, employee.Id, employee.FirstName, employee.LastName, employee.Position, Casino = employee.Casino.Name });
            }

            case "update_employee":
            {
                if (!user.IsStaff) return Forbidden();
                if (GetInt(args, "employeeId") is not int employeeId) return Error("employeeId is required.");
                var employee = await _db.Employees.Include(e => e.Casino).FirstOrDefaultAsync(e => e.Id == employeeId);
                if (employee is null) return Error($"Employee {employeeId} not found.");

                if (GetString(args, "firstName") is { Length: > 0 } fn)
                {
                    if (fn.Length > 100) return Error("firstName must be at most 100 characters.");
                    employee.FirstName = fn;
                }
                if (GetString(args, "lastName") is { Length: > 0 } ln)
                {
                    if (ln.Length > 100) return Error("lastName must be at most 100 characters.");
                    employee.LastName = ln;
                }
                if (GetString(args, "position") is { Length: > 0 } pos)
                {
                    if (pos.Length > 100) return Error("position must be at most 100 characters.");
                    employee.Position = pos;
                }
                if (GetInt(args, "casinoId") is int cid)
                {
                    if (!await _db.Casinos.AnyAsync(c => c.Id == cid)) return Error($"Casino {cid} not found.");
                    employee.CasinoId = cid;
                }

                await _db.SaveChangesAsync();
                await _db.Entry(employee).Reference(e => e.Casino).LoadAsync();
                return Json(new { ok = true, employee.Id, employee.FirstName, employee.LastName, employee.Position, Casino = employee.Casino.Name });
            }

            case "delete_employee":
            {
                if (!user.IsStaff) return Forbidden();
                if (GetInt(args, "employeeId") is not int employeeId) return Error("employeeId is required.");
                var employee = await _db.Employees.FirstOrDefaultAsync(e => e.Id == employeeId);
                if (employee is null) return Error($"Employee {employeeId} not found.");
                _db.Employees.Remove(employee);
                await _db.SaveChangesAsync();
                return Json(new { ok = true, deletedEmployeeId = employeeId });
            }

            case "create_table":
            {
                if (!user.IsStaff) return Forbidden();
                if (GetInt(args, "tableNumber") is not int tableNumber || tableNumber < 1) return Error("tableNumber must be a positive integer.");
                if (GetInt(args, "casinoId") is not int casinoId) return Error("casinoId is required.");
                if (!await _db.Casinos.AnyAsync(c => c.Id == casinoId)) return Error($"Casino {casinoId} not found.");
                if (GetInt(args, "gameId") is not int gameId) return Error("gameId is required.");
                if (!await _db.Games.AnyAsync(g => g.Id == gameId)) return Error($"Game {gameId} not found.");

                var isAvailable = GetBool(args, "isAvailable") ?? true;
                var minBet = GetDecimal(args, "minBet") ?? 10m;
                var maxBet = GetDecimal(args, "maxBet") ?? 500m;
                if (minBet is < 0.01m or > 1_000_000m || maxBet is < 0.01m or > 1_000_000m)
                    return Error("Bets must be between 0.01 and 1000000.");
                if (minBet > maxBet) return Error("minBet cannot be greater than maxBet.");

                var table = new Table
                {
                    TableNumber = tableNumber, IsAvailable = isAvailable, MinBet = minBet, MaxBet = maxBet,
                    CasinoId = casinoId, GameId = gameId
                };
                _db.Tables.Add(table);
                await _db.SaveChangesAsync();
                await _db.Entry(table).Reference(t => t.Casino).LoadAsync();
                await _db.Entry(table).Reference(t => t.Game).LoadAsync();
                return Json(new { ok = true, table.Id, table.TableNumber, table.IsAvailable, table.MinBet, table.MaxBet, Casino = table.Casino.Name, Game = table.Game.Name });
            }

            case "delete_table":
            {
                if (!user.IsStaff) return Forbidden();
                if (GetInt(args, "tableId") is not int tableId) return Error("tableId is required.");
                var table = await _db.Tables.FirstOrDefaultAsync(t => t.Id == tableId);
                if (table is null) return Error($"Table {tableId} not found.");
                _db.Tables.Remove(table);
                await _db.SaveChangesAsync();
                return Json(new { ok = true, deletedTableId = tableId });
            }

            case "create_player":
            {
                if (!user.IsStaff) return Forbidden();
                if (GetString(args, "firstName") is not { Length: > 0 } firstName) return Error("firstName is required.");
                if (firstName.Length > 100) return Error("firstName must be at most 100 characters.");
                if (GetString(args, "lastName") is not { Length: > 0 } lastName) return Error("lastName is required.");
                if (lastName.Length > 100) return Error("lastName must be at most 100 characters.");
                if (GetString(args, "email") is not { Length: > 0 } email) return Error("email is required.");
                if (!email.Contains('@')) return Error("email is not a valid address.");
                if (GetDate(args, "dateOfBirth") is not DateTime dob) return Error("dateOfBirth is required (ISO date).");

                var balance = GetDecimal(args, "balance") ?? 0;
                if (balance < 0) return Error("balance must be 0 or greater.");

                var player = new Player
                {
                    FirstName = firstName, LastName = lastName, Email = email,
                    DateOfBirth = dob, Balance = balance
                };
                _db.Players.Add(player);
                await _db.SaveChangesAsync();
                return Json(new { ok = true, player.Id, player.FirstName, player.LastName, player.Email, player.Balance, DateOfBirth = player.DateOfBirth.Date });
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
    private static readonly string[] GameTypeNames = Enum.GetNames<GameType>();

    private static TransactionType? ParseTransactionType(string? value) =>
        Enum.TryParse<TransactionType>(value, ignoreCase: true, out var t) ? t : null;

    private static GameType? ParseGameType(string? value) =>
        Enum.TryParse<GameType>(value, ignoreCase: true, out var t) ? t : null;

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
