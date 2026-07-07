using aspnet.Models;
using aspnet.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace aspnet.Controllers;

[Route("transakcije")]
public class TransactionController : Controller
{
    private readonly ITransactionRepository _repo;
    private readonly IPlayerRepository _playerRepo;

    public TransactionController(ITransactionRepository repo, IPlayerRepository playerRepo)
    {
        _repo = repo;
        _playerRepo = playerRepo;
    }

    [Route("")]
    [AllowAnonymous]
    public IActionResult Index() => View(_repo.GetAll());

    [Route("nova")]
    [Authorize(Roles = "Admin,Manager")]
    public IActionResult Create() => View(new Transaction { CreatedAt = DateTime.Now });

    [HttpPost]
    [Route("nova")]
    [Authorize(Roles = "Admin,Manager")]
    public IActionResult Create(Transaction transaction)
    {
        if (!ModelState.IsValid) return View(transaction);
        _repo.Create(transaction);
        return RedirectToAction("Index");
    }

    [Route("pretraga")]
    [AllowAnonymous]
    public IActionResult Search(string q)
    {
        if (string.IsNullOrWhiteSpace(q)) return Json(new List<object>());
        var results = _repo.Search(q).Select(t => new
        {
            id = t.Id,
            playerId = t.PlayerId,
            playerName = $"{t.Player?.FirstName} {t.Player?.LastName}",
            amount = t.Amount,
            type = t.Type.ToString(),
            createdAt = t.CreatedAt.ToString("dd.MM.yyyy HH:mm")
        });
        return Json(results);
    }
}
