using aspnet.Models;
using aspnet.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace aspnet.Controllers;

[Route("stolovi")]
public class TableController : Controller
{
    private readonly ITableRepository _repo;

    public TableController(ITableRepository repo) => _repo = repo;

    [Route("")]
    public IActionResult Index() => View(_repo.GetAll());

    [Route("{id:int}")]
    public IActionResult Details(int id)
    {
        var table = _repo.GetById(id);
        if (table is null) return NotFound();
        return View(table);
    }

    [Route("novi")]
    public IActionResult Create() => View(new Table());

    [HttpPost]
    [Route("novi")]
    public IActionResult Create(Table table)
    {
        if (!ModelState.IsValid) return View(table);
        _repo.Create(table);
        return RedirectToAction("Details", new { id = table.Id });
    }

    [Route("{id:int}/uredi")]
    public IActionResult Edit(int id)
    {
        var table = _repo.GetById(id);
        if (table is null) return NotFound();
        return View(table);
    }

    [HttpPost]
    [Route("{id:int}/uredi")]
    public IActionResult Edit(int id, Table table)
    {
        if (!ModelState.IsValid) return View(table);
        _repo.Update(table);
        return RedirectToAction("Details", new { id });
    }

    [HttpPost]
    [Route("{id:int}/obrisi")]
    public IActionResult Delete(int id)
    {
        _repo.Delete(id);
        return RedirectToAction("Index");
    }

    [Route("pretraga")]
    public IActionResult Search(string q)
    {
        if (string.IsNullOrWhiteSpace(q)) return Json(new List<object>());
        var results = _repo.Search(q).Select(t => new
        {
            id = t.Id,
            tableNumber = t.TableNumber,
            casino = t.Casino?.Name,
            game = t.Game?.Name,
            isAvailable = t.IsAvailable,
            minBet = t.MinBet,
            maxBet = t.MaxBet
        });
        return Json(results);
    }

    [Route("autocomplete")]
    public IActionResult Autocomplete(string q)
    {
        if (string.IsNullOrWhiteSpace(q)) return Json(new List<object>());
        var results = _repo.Search(q).Select(t => new
        {
            id = t.Id,
            label = $"Stol #{t.TableNumber} – {t.Casino?.Name} ({t.Game?.Name})"
        });
        return Json(results);
    }
}
