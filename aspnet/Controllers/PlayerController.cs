using aspnet.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace aspnet.Controllers;

public class PlayerController : Controller
{
    private readonly IPlayerRepository _repo;

    public PlayerController(IPlayerRepository repo) => _repo = repo;

    public IActionResult Index() => View(_repo.GetAll());

    public IActionResult Details(int id)
    {
        var player = _repo.GetById(id);
        if (player is null) return NotFound();
        return View(player);
    }
}
