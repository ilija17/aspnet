using System.Diagnostics;
using aspnet.Models;
using aspnet.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace aspnet.Controllers;

public class HomeController : Controller
{
    private readonly ICasinoRepository _casinos;

    public HomeController(ICasinoRepository casinos) => _casinos = casinos;

    // Casino Floor — custom page showing all casinos with live table availability
    public IActionResult Index() => View(_casinos.GetAll());

    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
