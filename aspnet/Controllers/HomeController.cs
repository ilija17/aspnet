// Handles the root route. Index renders the Casino Floor custom page showing live table availability.
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using aspnet.Data;
using aspnet.Models;
using aspnet.Repositories;
using aspnet.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace aspnet.Controllers;

public class HomeController : Controller
{
    private readonly ICasinoRepository _casinos;

    public HomeController(ICasinoRepository casinos) => _casinos = casinos;

    // Casino Floor — custom page showing all casinos with live table availability
    public IActionResult Index() => View(_casinos.GetAll());

    // Waitlist prijava: sprema email i šalje jednokratni potvrdni mail.
    // Pozicija je stvarni broj u tablici + 12000 da se poklapa s "Join 12,000+" na stranici.
    [HttpPost]
    public async Task<IActionResult> Waitlist(
        string? email,
        [FromServices] CasinoDbContext db,
        [FromServices] MailService mail,
        [FromServices] ILogger<HomeController> logger)
    {
        email = email?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(email) || email.Length > 320 || !new EmailAddressAttribute().IsValid(email))
        {
            return BadRequest(new { error = "Please enter a valid email address." });
        }

        var existing = await db.WaitlistEntries.FirstOrDefaultAsync(w => w.Email == email);
        if (existing is not null)
        {
            var existingPosition = 12000 + await db.WaitlistEntries.CountAsync(w => w.Id <= existing.Id);
            return Ok(new { position = existingPosition, alreadyJoined = true });
        }

        db.WaitlistEntries.Add(new WaitlistEntry { Email = email });
        await db.SaveChangesAsync();

        var position = 12000 + await db.WaitlistEntries.CountAsync();
        try
        {
            await mail.SendWaitlistConfirmationAsync(email, position);
        }
        catch (Exception ex)
        {
            // Prijava je spremljena; mail je best-effort
            logger.LogError(ex, "Failed to send waitlist confirmation to {Email}", email);
        }

        return Ok(new { position, alreadyJoined = false });
    }

    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
