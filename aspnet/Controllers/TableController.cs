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
}
