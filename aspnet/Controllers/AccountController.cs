using System.Security.Claims;
using aspnet.Models;
using aspnet.Repositories;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace aspnet.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly IPlayerRepository _playerRepo;

    public AccountController(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        IPlayerRepository playerRepo)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _playerRepo = playerRepo;
    }

    // Svaki registrirani korisnik je ujedno igrač — bez ovoga se novi računi
    // (posebno oni preko Googlea) ne pojavljuju na popisu igrača
    private void EnsurePlayerExists(string email, string firstName, string lastName, DateTime dateOfBirth)
    {
        if (_playerRepo.GetByEmail(email) is not null) return;

        _playerRepo.Create(new Player
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            DateOfBirth = dateOfBirth,
            Balance = 0
        });
    }

    [AllowAnonymous]
    public IActionResult Register(string returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, string returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid) return View(model);

        var user = new AppUser
        {
            UserName = model.Email,
            Email = model.Email,
            FirstName = model.FirstName,
            LastName = model.LastName,
            OIB = model.OIB,
            JMBG = model.JMBG
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (result.Succeeded)
        {
            EnsurePlayerExists(model.Email, model.FirstName, model.LastName, model.DateOfBirth!.Value);
            await _signInManager.SignInAsync(user, isPersistent: false);
            return LocalRedirect(returnUrl ?? "/");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }

    [AllowAnonymous]
    public IActionResult Login(string returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid) return View(model);

        var result = await _signInManager.PasswordSignInAsync(
            model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);

        if (result.Succeeded)
        {
            return LocalRedirect(returnUrl ?? "/");
        }

        ModelState.AddModelError(string.Empty, "Neispravna prijava.");
        return View(model);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [AllowAnonymous]
    public IActionResult AccessDenied() => View();

    // ── Moj profil ───────────────────────────────────────────────────────────

    [Authorize]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return RedirectToAction(nameof(Login));

        var player = _playerRepo.GetByEmail(user.Email!);

        return View(new ProfileViewModel
        {
            Email = user.Email,
            FirstName = user.FirstName ?? player?.FirstName,
            LastName = user.LastName ?? player?.LastName,
            DateOfBirth = player?.DateOfBirth,
            OIB = user.OIB,
            JMBG = user.JMBG
        });
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return RedirectToAction(nameof(Login));

        model.Email = user.Email;
        if (!ModelState.IsValid) return View(model);

        user.FirstName = model.FirstName;
        user.LastName = model.LastName;
        user.OIB = model.OIB;
        user.JMBG = model.JMBG;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }

        // Drži zapis igrača usklađenim s računom (stariji računi ga možda nemaju)
        var player = _playerRepo.GetByEmail(user.Email!);
        if (player is null)
        {
            EnsurePlayerExists(user.Email!, model.FirstName, model.LastName, model.DateOfBirth!.Value);
        }
        else
        {
            player.FirstName = model.FirstName;
            player.LastName = model.LastName;
            player.DateOfBirth = model.DateOfBirth!.Value;
            _playerRepo.Update(player);
        }

        TempData["ProfileSaved"] = true;
        return RedirectToAction(nameof(Profile));
    }

    // ── Vanjska prijava (Google) ─────────────────────────────────────────────

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public IActionResult ExternalLogin(string provider, string returnUrl = null)
    {
        var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl });
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        return Challenge(properties, provider);
    }

    [AllowAnonymous]
    public async Task<IActionResult> ExternalLoginCallback(string returnUrl = null, string remoteError = null)
    {
        if (remoteError != null)
        {
            ModelState.AddModelError(string.Empty, $"Greška vanjskog providera: {remoteError}");
            return View("Login", new LoginViewModel());
        }

        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            return RedirectToAction(nameof(Login));
        }

        // Korisnik koji se već povezao s vanjskim providerom — direktna prijava
        var result = await _signInManager.ExternalLoginSignInAsync(
            info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);

        if (result.Succeeded)
        {
            return LocalRedirect(returnUrl ?? "/");
        }

        // Prva prijava — korisnik mora dovršiti registraciju (OIB, JMBG)
        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        return View("ExternalLogin", new ExternalLoginViewModel
        {
            Email = email,
            FirstName = info.Principal.FindFirstValue(ClaimTypes.GivenName),
            LastName = info.Principal.FindFirstValue(ClaimTypes.Surname),
            ProviderDisplayName = info.ProviderDisplayName,
            ReturnUrl = returnUrl
        });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExternalLoginConfirmation(ExternalLoginViewModel model)
    {
        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            return RedirectToAction(nameof(Login));
        }

        if (!ModelState.IsValid)
        {
            model.ProviderDisplayName = info.ProviderDisplayName;
            return View("ExternalLogin", model);
        }

        var user = new AppUser
        {
            UserName = model.Email,
            Email = model.Email,
            FirstName = model.FirstName,
            LastName = model.LastName,
            OIB = model.OIB,
            JMBG = model.JMBG
        };

        var result = await _userManager.CreateAsync(user);

        if (result.Succeeded)
        {
            result = await _userManager.AddLoginAsync(user, info);

            if (result.Succeeded)
            {
                EnsurePlayerExists(model.Email, model.FirstName, model.LastName, model.DateOfBirth!.Value);
                await _signInManager.SignInAsync(user, isPersistent: false);
                return LocalRedirect(model.ReturnUrl ?? "/");
            }
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        model.ProviderDisplayName = info.ProviderDisplayName;
        return View("ExternalLogin", model);
    }
}
