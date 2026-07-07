using aspnet.Models;
using aspnet.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace aspnet.Controllers;

[Route("rezervacije")]
public class ReservationController : Controller
{
    private readonly IReservationRepository _repo;

    public ReservationController(IReservationRepository repo) => _repo = repo;

    [Route("")]
    [AllowAnonymous]
    public IActionResult Index() => View(_repo.GetAll());

    [Route("{id:int}")]
    [Authorize]
    public IActionResult Details(int id)
    {
        var reservation = _repo.GetById(id);
        if (reservation is null) return NotFound();
        return View(reservation);
    }

    [Route("nova")]
    [Authorize(Roles = "Admin,Manager")]
    public IActionResult Create() => View(new Reservation { ReservedAt = DateTime.Now });

    [HttpPost]
    [Route("nova")]
    [Authorize(Roles = "Admin,Manager")]
    public IActionResult Create(Reservation reservation)
    {
        if (!ModelState.IsValid) return View(reservation);
        _repo.Create(reservation);
        return RedirectToAction("Details", new { id = reservation.Id });
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
        var results = _repo.Search(q).Select(r => new
        {
            id = r.Id,
            playerId = r.PlayerId,
            playerName = $"{r.Player?.FirstName} {r.Player?.LastName}",
            tableNumber = r.Table?.TableNumber,
            casinoName = r.Table?.Casino?.Name,
            gameName = r.Table?.Game?.Name,
            reservedAt = r.ReservedAt.ToString("dd.MM.yyyy HH:mm")
        });
        return Json(results);
    }
}
