using aspnet.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace aspnet.Controllers;

[Route("igre")]
public class GameController : Controller
{
    private readonly IGameRepository _repo;

    public GameController(IGameRepository repo) => _repo = repo;

    [Route("")]
    public IActionResult Index() => View(_repo.GetAll());

    [Route("{id:int}")]
    public IActionResult Details(int id)
    {
        var game = _repo.GetById(id);
        if (game is null) return NotFound();
        return View(game);
    }
}
