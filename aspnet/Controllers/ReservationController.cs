using aspnet.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace aspnet.Controllers;

public class ReservationController : Controller
{
    private readonly IReservationRepository _repo;

    public ReservationController(IReservationRepository repo) => _repo = repo;

    public IActionResult Index() => View(_repo.GetAll());

    public IActionResult Details(int id)
    {
        var reservation = _repo.GetById(id);
        if (reservation is null) return NotFound();
        return View(reservation);
    }
}
