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
}
