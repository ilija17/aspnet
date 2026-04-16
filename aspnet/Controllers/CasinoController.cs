using aspnet.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace aspnet.Controllers;

public class CasinoController : Controller
{
    private readonly ICasinoRepository _repo;

    public CasinoController(ICasinoRepository repo) => _repo = repo;

    public IActionResult Index() => View(_repo.GetAll());

    public IActionResult Details(int id)
    {
        var casino = _repo.GetById(id);
        if (casino is null) return NotFound();
        return View(casino);
    }
}
