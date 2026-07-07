using aspnet.Models;
using aspnet.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace aspnet.Controllers;

[Route("djelatnici")]
public class EmployeeController : Controller
{
    private readonly IEmployeeRepository _repo;

    public EmployeeController(IEmployeeRepository repo) => _repo = repo;

    [Route("")]
    [AllowAnonymous]
    public IActionResult Index() => View(_repo.GetAll());

    [Route("{id:int}")]
    [Authorize]
    public IActionResult Details(int id)
    {
        var employee = _repo.GetById(id);
        if (employee is null) return NotFound();
        return View(employee);
    }

    [Route("novi")]
    [Authorize(Roles = "Admin,Manager")]
    public IActionResult Create() => View(new Employee());

    [HttpPost]
    [Route("novi")]
    [Authorize(Roles = "Admin,Manager")]
    public IActionResult Create(Employee employee)
    {
        if (!ModelState.IsValid) return View(employee);
        _repo.Create(employee);
        return RedirectToAction("Details", new { id = employee.Id });
    }

    [Route("{id:int}/uredi")]
    [Authorize(Roles = "Admin,Manager")]
    public IActionResult Edit(int id)
    {
        var employee = _repo.GetById(id);
        if (employee is null) return NotFound();
        return View(employee);
    }

    [HttpPost]
    [Route("{id:int}/uredi")]
    [Authorize(Roles = "Admin,Manager")]
    public IActionResult Edit(int id, Employee employee)
    {
        if (!ModelState.IsValid) return View(employee);
        _repo.Update(employee);
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
        var results = _repo.Search(q).Select(e => new
        {
            id = e.Id,
            firstName = e.FirstName,
            lastName = e.LastName,
            position = e.Position,
            casinoId = e.CasinoId,
            casinoName = e.Casino?.Name
        });
        return Json(results);
    }
}
