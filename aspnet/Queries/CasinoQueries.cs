using aspnet.Data;
using aspnet.Models;

namespace aspnet.Queries;

// ══════════════════════════════════════════════════════════════════════════════
// LINQ QUERY REFERENCE
// ══════════════════════════════════════════════════════════════════════════════
//
// WHAT IS LINQ?
//   Language Integrated Query – extension methods on IEnumerable<T> that let
//   you filter, transform, sort, and aggregate collections in a readable,
//   SQL-like syntax directly in C#.
//
// DEFERRED vs IMMEDIATE execution
//   Most LINQ methods (Where, Select, OrderBy…) are LAZY – they build a
//   pipeline but do NOT run until you iterate or call a terminating method:
//   ToList / ToArray / First / Count / Sum / Any …
//
// async / await
//   Mark a method async and return Task<T>.  Inside it, use await on I/O
//   operations so the calling thread is released while waiting (e.g. for a
//   DB response).  EF Core provides async variants of every terminating
//   method: ToListAsync, FirstOrDefaultAsync, SumAsync, AnyAsync …
//   Always prefer them in controller actions to keep the server responsive.
//
//   Sync  (blocks the thread): db.Tables.Where(…).ToList()
//   Async (frees the thread):  await db.Tables.Where(…).ToListAsync()
//
// ══════════════════════════════════════════════════════════════════════════════

public static class CasinoQueries
{
    // Entry point – runs all queries against the in-memory seed data and
    // prints results to the console.
    public static void RunAndPrint(CasinoSeedData data)
    {
        // Derive a flat table list once; used by several queries below.
        var allTables       = data.Casinos.SelectMany(c => c.Tables).ToList();
        var allTransactions = data.Players.SelectMany(p => p.Transactions).ToList();

        PrintAvailableTables(allTables);
        PrintCasinoNames(data.Casinos);
        PrintPlayersByBalance(data.Players);
        PrintHighRollerEmails(data.Players);
        PrintAllTransactions(allTransactions);
        PrintAggregates(allTransactions, data.Casinos);
        PrintBiggestWin(allTransactions);
        PrintTablesByGame(allTables);
        PrintPokerAvailability(data.Casinos);
        PrintReservationSummary(data.Reservations);
    }

    // ── 1. WHERE – filter ─────────────────────────────────────────────────────
    // Keeps only the tables where IsAvailable == true.
    private static void PrintAvailableTables(List<Table> allTables)
    {
        var available = allTables
            .Where(t => t.IsAvailable)
            .ToList();

        Console.WriteLine("=== 1. Available tables ===");
        foreach (var t in available)
            Console.WriteLine($"  Table {t.TableNumber} | {t.Game.Name} | min bet: {t.MinBet} EUR");
    }

    // ── 2. SELECT – project / transform ──────────────────────────────────────
    // Extracts just the Name string from each Casino object.
    private static void PrintCasinoNames(List<Casino> casinos)
    {
        var names = casinos
            .Select(c => c.Name)
            .ToList();

        Console.WriteLine("\n=== 2. Casino names ===");
        foreach (var name in names)
            Console.WriteLine($"  {name}");
    }

    // ── 3. ORDERBY – sort ─────────────────────────────────────────────────────
    // Sorts players descending by Balance (highest balance = VIP first).
    private static void PrintPlayersByBalance(List<Player> players)
    {
        var sorted = players
            .OrderByDescending(p => p.Balance)
            .ToList();

        Console.WriteLine("\n=== 3. Players by balance (VIP order) ===");
        foreach (var p in sorted)
            Console.WriteLine($"  {p.FirstName} {p.LastName} – {p.Balance} EUR");
    }

    // ── 4. WHERE + SELECT (chained) ───────────────────────────────────────────
    // First filters to high-rollers, then projects down to just their email.
    private static void PrintHighRollerEmails(List<Player> players)
    {
        var emails = players
            .Where(p => p.Balance > 1000)
            .Select(p => p.Email)
            .ToList();

        Console.WriteLine("\n=== 4. High-roller emails (balance > 1000) ===");
        foreach (var email in emails)
            Console.WriteLine($"  {email}");
    }

    // ── 5. SELECTMANY – flatten nested collections ────────────────────────────
    // Each player has a List<Transaction>; SelectMany flattens all of them
    // into a single sequence.
    private static void PrintAllTransactions(List<Transaction> allTransactions)
    {
        Console.WriteLine("\n=== 5. All transactions ===");
        foreach (var t in allTransactions)
            Console.WriteLine($"  {t.Player.FirstName} | {t.Type} | {t.Amount} EUR | {t.CreatedAt:d}");
    }

    // ── 6. SUM / COUNT – aggregation ─────────────────────────────────────────
    // Sum is a terminating call – it executes the query immediately and returns
    // a single scalar value (not a collection).
    private static void PrintAggregates(List<Transaction> allTransactions, List<Casino> casinos)
    {
        var totalWon = allTransactions
            .Where(t => t.Type == TransactionType.Win)
            .Sum(t => t.Amount);

        // Anonymous projection: new { Name, TableCount }
        var tableCountPerCasino = casinos
            .Select(c => new { c.Name, TableCount = c.Tables.Count })
            .ToList();

        Console.WriteLine($"\n=== 6. Total won across all players: {totalWon} EUR ===");
        foreach (var entry in tableCountPerCasino)
            Console.WriteLine($"  {entry.Name}: {entry.TableCount} tables");
    }

    // ── 7. FIRSTORDEFAULT ────────────────────────────────────────────────────
    // Returns the first match or null – never throws on an empty sequence.
    private static void PrintBiggestWin(List<Transaction> allTransactions)
    {
        var biggestWin = allTransactions
            .Where(t => t.Type == TransactionType.Win)
            .OrderByDescending(t => t.Amount)
            .FirstOrDefault();

        Console.WriteLine("\n=== 7. Biggest single win ===");
        if (biggestWin != null)
            Console.WriteLine($"  {biggestWin.Amount} EUR by player ID {biggestWin.PlayerId}");
    }

    // ── 8. GROUPBY ───────────────────────────────────────────────────────────
    // Groups available tables by their GameType enum value.
    // Result is IEnumerable<IGrouping<GameType, Table>>.
    private static void PrintTablesByGame(List<Table> allTables)
    {
        var groups = allTables
            .Where(t => t.IsAvailable)
            .GroupBy(t => t.Game.Type)
            .ToList();

        Console.WriteLine("\n=== 8. Available tables grouped by game ===");
        foreach (var group in groups)
        {
            Console.WriteLine($"  [{group.Key}]");
            foreach (var t in group)
                Console.WriteLine($"    Table {t.TableNumber} | min: {t.MinBet} EUR");
        }
    }

    // ── 9. ANY ───────────────────────────────────────────────────────────────
    // Any is a short-circuit terminator – stops as soon as one match is found.
    private static void PrintPokerAvailability(List<Casino> casinos)
    {
        bool pokerAvailable = casinos
            .Any(c => c.Tables.Any(t => t.Game.Type == GameType.Poker && t.IsAvailable));

        Console.WriteLine($"\n=== 9. Poker table available right now: {pokerAvailable} ===");
    }

    // ── 10. SELECT projection (implicit join via navigation properties) ───────
    // Navigation properties (Player, Table.Casino, Table.Game) are already
    // loaded in memory, so no explicit join syntax is needed.
    private static void PrintReservationSummary(List<Reservation> reservations)
    {
        var summary = reservations
            .Select(r => new
            {
                Player = $"{r.Player.FirstName} {r.Player.LastName}",
                Casino = r.Table.Casino?.Name ?? "N/A",
                Game   = r.Table.Game.Name,
                At     = r.ReservedAt
            })
            .OrderBy(r => r.At)
            .ToList();

        Console.WriteLine("\n=== 10. Reservation summary ===");
        foreach (var r in summary)
            Console.WriteLine($"  {r.Player} @ {r.Casino} – {r.Game} – {r.At:g}");

        Console.WriteLine();
    }

    // ── async / await DB equivalents ─────────────────────────────────────────
    // The methods above run on in-memory lists (no I/O → no async needed).
    // When querying the real database through EF Core, swap the terminating
    // call for its async counterpart and mark the method async Task<…>:
    //
    //   // Sync – blocks the thread for the full SQL round-trip
    //   List<Table> tables = db.Tables.Where(t => t.IsAvailable).ToList();
    //
    //   // Async – releases the thread while SQL Server works
    //   List<Table> tables = await db.Tables
    //       .Include(t => t.Game)
    //       .Where(t => t.IsAvailable)
    //       .ToListAsync();
    //
    //   decimal won = await db.Transactions
    //       .Where(t => t.Type == TransactionType.Win)
    //       .SumAsync(t => t.Amount);
}
