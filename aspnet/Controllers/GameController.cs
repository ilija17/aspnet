using aspnet.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace aspnet.Controllers;

public class GameController : Controller
{
    private readonly IGameRepository _repo;

    public GameController(IGameRepository repo) => _repo = repo;

    public IActionResult Index() => View(_repo.GetAll());

    public IActionResult Details(int id)
    {
        var game = _repo.GetById(id);
        if (game is null) return NotFound();
        return View(game);
    }
}
