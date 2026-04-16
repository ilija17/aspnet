using aspnet.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace aspnet.Controllers;

public class EmployeeController : Controller
{
    private readonly IEmployeeRepository _repo;

    public EmployeeController(IEmployeeRepository repo) => _repo = repo;

    public IActionResult Index() => View(_repo.GetAll());

    public IActionResult Details(int id)
    {
        var employee = _repo.GetById(id);
        if (employee is null) return NotFound();
        return View(employee);
    }
}
