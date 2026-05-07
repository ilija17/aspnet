using aspnet.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace aspnet.Controllers;

[Route("transakcije")]
public class TransactionController : Controller
{
    private readonly ITransactionRepository _repo;

    public TransactionController(ITransactionRepository repo) => _repo = repo;

    [Route("")]
    public IActionResult Index() => View(_repo.GetAll());
}
