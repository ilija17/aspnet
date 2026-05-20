using aspnet.Models;
using aspnet.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace aspnet.Controllers;

[Route("igraci")]
public class PlayerController : Controller
{
    private readonly IPlayerRepository _repo;

    public PlayerController(IPlayerRepository repo) => _repo = repo;

    [Route("")]
    public IActionResult Index() => View(_repo.GetAll());

    [Route("{id:int}")]
    public IActionResult Details(int id)
    {
        var player = _repo.GetById(id);
        if (player is null) return NotFound();
        return View(player);
    }

    [Route("novi")]
    public IActionResult Create() => View(new Player());

    [HttpPost]
    [Route("novi")]
    public IActionResult Create(Player player)
    {
        if (!ModelState.IsValid) return View(player);
        _repo.Create(player);
        return RedirectToAction("Details", new { id = player.Id });
    }

    [Route("{id:int}/uredi")]
    public IActionResult Edit(int id)
    {
        var player = _repo.GetById(id);
        if (player is null) return NotFound();
        return View(player);
    }

    [HttpPost]
    [Route("{id:int}/uredi")]
    public IActionResult Edit(int id, Player player)
    {
        if (!ModelState.IsValid) return View(player);
        _repo.Update(player);
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
        var results = _repo.Search(q).Select(p => new
        {
            id = p.Id,
            firstName = p.FirstName,
            lastName = p.LastName,
            email = p.Email,
            balance = p.Balance,
            reservationCount = p.Reservations.Count,
            transactionCount = p.Transactions.Count
        });
        return Json(results);
    }

    [Route("autocomplete")]
    public IActionResult Autocomplete(string q)
    {
        if (string.IsNullOrWhiteSpace(q)) return Json(new List<object>());
        var results = _repo.Search(q).Select(p => new
        {
            id = p.Id,
            label = $"{p.FirstName} {p.LastName} ({p.Email})"
        });
        return Json(results);
    }
}
