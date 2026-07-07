using aspnet.Models.DTO;
using aspnet.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace aspnet.Controllers;

[Route("pretraga")]
public class SearchController : Controller
{
    private readonly ICasinoRepository _casinos;
    private readonly IPlayerRepository _players;
    private readonly IGameRepository _games;
    private readonly ITableRepository _tables;
    private readonly IEmployeeRepository _employees;
    private readonly IReservationRepository _reservations;
    private readonly ITransactionRepository _transactions;

    public SearchController(
        ICasinoRepository casinos,
        IPlayerRepository players,
        IGameRepository games,
        ITableRepository tables,
        IEmployeeRepository employees,
        IReservationRepository reservations,
        ITransactionRepository transactions)
    {
        _casinos = casinos;
        _players = players;
        _games = games;
        _tables = tables;
        _employees = employees;
        _reservations = reservations;
        _transactions = transactions;
    }

    [HttpGet("")]
    public IActionResult Index(string q, int limit = 5)
    {
        if (string.IsNullOrWhiteSpace(q))
            return Json(new GlobalSearchResultDTO());

        var hits = new List<SearchHitDTO>();

        hits.AddRange(_casinos.Search(q).Take(limit).Select(c => new SearchHitDTO
        {
            Type = "Casinos",
            Id = c.Id,
            Title = c.Name,
            Subtitle = c.Address,
            Url = $"/kasina/{c.Id}"
        }));

        hits.AddRange(_players.Search(q).Take(limit).Select(p => new SearchHitDTO
        {
            Type = "Players",
            Id = p.Id,
            Title = $"{p.FirstName} {p.LastName}",
            Subtitle = p.Email,
            Url = $"/igraci/{p.Id}"
        }));

        hits.AddRange(_games.Search(q).Take(limit).Select(g => new SearchHitDTO
        {
            Type = "Games",
            Id = g.Id,
            Title = g.Name,
            Subtitle = g.Type.ToString(),
            Url = $"/igre/{g.Id}"
        }));

        hits.AddRange(_tables.Search(q).Take(limit).Select(t => new SearchHitDTO
        {
            Type = "Tables",
            Id = t.Id,
            Title = $"Table #{t.TableNumber}",
            Subtitle = $"{t.Casino?.Name ?? "?"} — {t.Game?.Name ?? "?"}",
            Url = $"/stolovi/{t.Id}"
        }));

        hits.AddRange(_employees.Search(q).Take(limit).Select(e => new SearchHitDTO
        {
            Type = "Employees",
            Id = e.Id,
            Title = $"{e.FirstName} {e.LastName}",
            Subtitle = e.Position,
            Url = $"/djelatnici/{e.Id}"
        }));

        hits.AddRange(_reservations.Search(q).Take(limit).Select(r => new SearchHitDTO
        {
            Type = "Reservations",
            Id = r.Id,
            Title = $"#{r.Id} — {r.Player?.FirstName} {r.Player?.LastName}",
            Subtitle = r.ReservedAt.ToString("d. MMM yyyy"),
            Url = $"/rezervacije/{r.Id}"
        }));

        hits.AddRange(_transactions.Search(q).Take(limit).Select(t => new SearchHitDTO
        {
            Type = "Transactions",
            Id = t.Id,
            Title = $"{t.Type} — €{t.Amount:F2}",
            Subtitle = $"{t.Player?.FirstName} {t.Player?.LastName}",
            Url = $"/transakcije"
        }));

        return Json(new GlobalSearchResultDTO { Hits = hits });
    }
}
