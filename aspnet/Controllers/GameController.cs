using aspnet.Models;
using aspnet.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace aspnet.Controllers;

[Route("igre")]
public class GameController : Controller
{
    private readonly IGameRepository _repo;

    public GameController(IGameRepository repo) => _repo = repo;

    [Route("")]
    [AllowAnonymous]
    public IActionResult Index() => View(_repo.GetAll());

    [Route("{id:int}")]
    [Authorize]
    public IActionResult Details(int id)
    {
        var game = _repo.GetById(id);
        if (game is null) return NotFound();
        return View(game);
    }

    [Route("nova")]
    [Authorize(Roles = "Admin,Manager")]
    public IActionResult Create() => View(new Game());

    [HttpPost]
    [Route("nova")]
    [Authorize(Roles = "Admin,Manager")]
    public IActionResult Create(Game game)
    {
        if (!ModelState.IsValid) return View(game);
        _repo.Create(game);
        return RedirectToAction("Details", new { id = game.Id });
    }

    [Route("{id:int}/uredi")]
    [Authorize(Roles = "Admin,Manager")]
    public IActionResult Edit(int id)
    {
        var game = _repo.GetById(id);
        if (game is null) return NotFound();
        return View(game);
    }

    [HttpPost]
    [Route("{id:int}/uredi")]
    [Authorize(Roles = "Admin,Manager")]
    public IActionResult Edit(int id, Game game)
    {
        if (!ModelState.IsValid) return View(game);
        _repo.Update(game);
        return RedirectToAction("Details", new { id });
    }

    [HttpPost]
    [Route("{id:int}/obrisi")]
    [Authorize(Roles = "Admin")]
    public IActionResult Delete(int id)
    {
        _repo.Delete(id);
        return RedirectToAction("Index");
    }

    [Route("pretraga")]
    [AllowAnonymous]
    public IActionResult Search(string q)
    {
        if (string.IsNullOrWhiteSpace(q)) return Json(new List<object>());
        var results = _repo.Search(q).Select(g => new
        {
            id = g.Id,
            name = g.Name,
            type = g.Type.ToString(),
            minBet = g.MinBet,
            maxBet = g.MaxBet
        });
        return Json(results);
    }

    [Route("autocomplete")]
    [AllowAnonymous]
    public IActionResult Autocomplete(string q)
    {
        if (string.IsNullOrWhiteSpace(q)) return Json(new List<object>());
        var results = _repo.Search(q).Select(g => new
        {
            id = g.Id,
            label = $"{g.Name} ({g.Type})"
        });
        return Json(results);
    }
}
