using aspnet.Data;
using aspnet.Repositories;
using ModelContextProtocol.Server;

namespace aspnet.Mcp;

public sealed class CasinoTools
{
    private readonly ICasinoRepository _casinos;
    private readonly IPlayerRepository _players;
    private readonly IGameRepository _games;
    private readonly ITableRepository _tables;
    private readonly IEmployeeRepository _employees;
    private readonly IReservationRepository _reservations;
    private readonly ITransactionRepository _transactions;
    private readonly CasinoDbContext _db;

    public CasinoTools(
        ICasinoRepository casinos,
        IPlayerRepository players,
        IGameRepository games,
        ITableRepository tables,
        IEmployeeRepository employees,
        IReservationRepository reservations,
        ITransactionRepository transactions,
        CasinoDbContext db)
    {
        _casinos = casinos;
        _players = players;
        _games = games;
        _tables = tables;
        _employees = employees;
        _reservations = reservations;
        _transactions = transactions;
        _db = db;
    }

    /// <summary>
    /// Searches across all entities (Casinos, Players, Games, Tables, Employees, Reservations, Transactions) and returns matching results grouped by type.
    /// </summary>
    /// <param name="q">Query string to search for across all entity types</param>
    /// <param name="limit">Maximum results per category (default: 5)</param>
    [McpServerTool(Title = "Search entities")]
    public string SearchAll(string q, int limit = 5)
    {
        if (string.IsNullOrWhiteSpace(q)) return "{\"hits\":[]}";

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("{\"hits\":[");
        var first = true;

        void AddHits(string type, IEnumerable<object> items)
        {
            foreach (dynamic item in items)
            {
                if (!first) sb.Append(',');
                first = false;
                var title = type switch
                {
                    "Casinos" => (string)item.name,
                    "Players" => $"{item.firstName} {item.lastName}",
                    "Games" => (string)item.name,
                    "Tables" => $"Table #{item.tableNumber}",
                    "Employees" => $"{item.firstName} {item.lastName}",
                    "Reservations" => $"#{item.id} — {item.playerName}",
                    "Transactions" => $"{item.type} — €{item.amount:F2}",
                    _ => ""
                };
                var subtitle = type switch
                {
                    "Casinos" => (string)item.address,
                    "Players" => (string)item.email,
                    "Games" => item.type.ToString(),
                    "Tables" => $"{item.casinoName} — {item.gameName}",
                    "Employees" => (string)item.position,
                    "Reservations" => item.reservedAt != null ? item.reservedAt.ToString() : "",
                    "Transactions" => (string)item.playerName,
                    _ => ""
                };
                var url = type switch
                {
                    "Casinos" => $"/kasina/{item.id}",
                    "Players" => $"/igraci/{item.id}",
                    "Games" => $"/igre/{item.id}",
                    "Tables" => $"/stolovi/{item.id}",
                    "Employees" => $"/djelatnici/{item.id}",
                    "Reservations" => $"/rezervacije/{item.id}",
                    "Transactions" => $"/transakcije",
                    _ => ""
                };
                sb.Append($"{{\"type\":\"{type}\",\"id\":{item.id},\"title\":\"{title}\",\"subtitle\":\"{subtitle}\",\"url\":\"{url}\"}}");
            }
        }

        AddHits("Casinos", _casinos.Search(q).Take(limit).Select(c => new { c.Id, c.Name, c.Address }));
        AddHits("Players", _players.Search(q).Take(limit).Select(p => new { p.Id, p.FirstName, p.LastName, p.Email }));
        AddHits("Games", _games.Search(q).Take(limit).Select(g => new { g.Id, g.Name, Type = g.Type.ToString() }));
        AddHits("Tables", _tables.Search(q).Take(limit).Select(t => new { t.Id, t.TableNumber, casinoName = t.Casino?.Name ?? "?", gameName = t.Game?.Name ?? "?" }));
        AddHits("Employees", _employees.Search(q).Take(limit).Select(e => new { e.Id, e.FirstName, e.LastName, e.Position }));
        AddHits("Reservations", _reservations.Search(q).Take(limit).Select(r => new { r.Id, playerName = $"{r.Player?.FirstName} {r.Player?.LastName}", reservedAt = r.ReservedAt.ToString("d.MMM.yyyy") }));
        AddHits("Transactions", _transactions.Search(q).Take(limit).Select(t => new { t.Id, Type = t.Type.ToString(), t.Amount, playerName = $"{t.Player?.FirstName} {t.Player?.LastName}" }));

        sb.Append("]}");
        return sb.ToString();
    }

    /// <summary>
    /// Returns a summary of database entity counts (row counts for each table).
    /// </summary>
    [McpServerTool(Title = "Get entity counts")]
    public string GetEntityCounts()
    {
        return System.Text.Json.JsonSerializer.Serialize(new
        {
            casinos = _db.Casinos.Count(),
            players = _db.Players.Count(),
            games = _db.Games.Count(),
            tables = _db.Tables.Count(),
            employees = _db.Employees.Count(),
            reservations = _db.Reservations.Count(),
            transactions = _db.Transactions.Count()
        });
    }

    /// <summary>
    /// Returns a list of all casinos with their IDs, names, and addresses.
    /// </summary>
    [McpServerTool(Title = "List casinos")]
    public string ListCasinos()
    {
        var casinos = _casinos.GetAll().Select(c => new { c.Id, c.Name, c.Address, c.LicenseNumber, c.FoundedDate });
        return System.Text.Json.JsonSerializer.Serialize(casinos);
    }

    /// <summary>
    /// Returns a list of all players with their IDs, names, emails, and balances.
    /// </summary>
    [McpServerTool(Title = "List players")]
    public string ListPlayers()
    {
        var players = _players.GetAll().Select(p => new { p.Id, p.FirstName, p.LastName, p.Email, p.Balance });
        return System.Text.Json.JsonSerializer.Serialize(players);
    }

    /// <summary>
    /// Returns all tables grouped by casino, showing which are available.
    /// </summary>
    [McpServerTool(Title = "Get table availability")]
    public string GetTableAvailability()
    {
        var tables = _tables.GetAll()
            .GroupBy(t => t.Casino!.Name)
            .Select(g => new
            {
                casino = g.Key,
                totalTables = g.Count(),
                available = g.Count(t => t.IsAvailable),
                tables = g.Select(t => new
                {
                    t.Id,
                    t.TableNumber,
                    t.IsAvailable,
                    game = t.Game!.Name,
                    t.MinBet,
                    t.MaxBet
                })
            });
        return System.Text.Json.JsonSerializer.Serialize(tables);
    }

    /// <summary>
    /// Returns the database schema and application route map for the Casino Management System.
    /// </summary>
    [McpServerTool(Title = "Get database schema")]
    public string GetDatabaseSchema()
    {
        return @"
Casino Management System - Database Schema

Entities:
  Casino (Id, Name, Address, LicenseNumber, FoundedDate)
    ├── Tables (1:N)
    ├── Employees (1:N)
    └── Attachments (1:N)

  Player (Id, FirstName, LastName, Email, DateOfBirth, Balance)
    ├── Transactions (1:N)
    └── Reservations (1:N)

  Game (Id, Name, Type [Slots|Poker|Blackjack|Roulette], MinBet, MaxBet, Description)
    └── Tables (1:N)

  Table (Id, TableNumber, IsAvailable, MinBet, MaxBet, CasinoId, GameId)
    ├── Casino (N:1)
    ├── Game (N:1)
    └── Reservations (1:N)

  Employee (Id, FirstName, LastName, Position, CasinoId)
    └── Casino (N:1)

  Transaction (Id, Amount, Type [Deposit|Withdrawal|Bet|Win], CreatedAt, PlayerId)
    └── Player (N:1)

  Reservation (Id, ReservedAt, PlayerId, TableId)
    ├── Player (N:1)
    └── Table (N:1)

App Routes:
  /kasina           - Casinos CRUD + search
  /igraci           - Players CRUD + search
  /igre             - Games CRUD + search
  /stolovi          - Tables CRUD + search
  /djelatnici       - Employees CRUD + search
  /rezervacije      - Reservations + search
  /transakcije      - Transactions + search
  /pretraga?q=      - Global search across all entities
  /api/logs         - View application logs (Admin/Manager)
  /api/casino, /api/player, /api/game, etc. - RESTful APIs
";
    }
}
