using aspnet.Models;
using aspnet.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace aspnet.Controllers;

[Route("kasina")]
public class CasinoController : Controller
{
    private readonly ICasinoRepository _repo;

    public CasinoController(ICasinoRepository repo) => _repo = repo;

    [Route("")]
    public IActionResult Index() => View(_repo.GetAll());

    [Route("{id:int}")]
    public IActionResult Details(int id)
    {
        var casino = _repo.GetById(id);
        if (casino is null) return NotFound();
        return View(casino);
    }

    [Route("novi")]
    public IActionResult Create() => View(new Casino());

    [HttpPost]
    [Route("novi")]
    public IActionResult Create(Casino casino)
    {
        if (!ModelState.IsValid) return View(casino);
        _repo.Create(casino);
        return RedirectToAction("Details", new { id = casino.Id });
    }

    [Route("{id:int}/uredi")]
    public IActionResult Edit(int id)
    {
        var casino = _repo.GetById(id);
        if (casino is null) return NotFound();
        return View(casino);
    }

    [HttpPost]
    [Route("{id:int}/uredi")]
    public IActionResult Edit(int id, Casino casino)
    {
        if (!ModelState.IsValid) return View(casino);
        _repo.Update(casino);
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
        var results = _repo.Search(q).Select(c => new
        {
            id = c.Id,
            name = c.Name,
            address = c.Address,
            licenseNumber = c.LicenseNumber
        });
        return Json(results);
    }

    [Route("autocomplete")]
    public IActionResult Autocomplete(string q)
    {
        if (string.IsNullOrWhiteSpace(q)) return Json(new List<object>());
        var results = _repo.Search(q).Select(c => new
        {
            id = c.Id,
            label = c.Name
        });
        return Json(results);
    }
}
