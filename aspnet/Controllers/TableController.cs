using aspnet.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace aspnet.Controllers;

public class TableController : Controller
{
    private readonly ITableRepository _repo;

    public TableController(ITableRepository repo) => _repo = repo;

    public IActionResult Index() => View(_repo.GetAll());

    public IActionResult Details(int id)
    {
        var table = _repo.GetById(id);
        if (table is null) return NotFound();
        return View(table);
    }
}
